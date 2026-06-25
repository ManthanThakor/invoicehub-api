using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Sales
{

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customers;
        private readonly IInvoiceRepository _invoices;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CustomerService> _log;

        public CustomerService(
            ICustomerRepository customers, IInvoiceRepository invoices,
            IAuditService audit, IUnitOfWork uow, ILogger<CustomerService> log)
        {
            _customers = customers; _invoices = invoices;
            _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<CustomerListDto>>> GetListAsync(
            Guid tenantId, CustomerFilterDto filter, CancellationToken ct = default)
        {
            var query = _customers.Query(tenantId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(c =>
                    c.Name.Contains(filter.Search) ||
                    (c.Email != null && c.Email.Contains(filter.Search)) ||
                    (c.Phone != null && c.Phone.Contains(filter.Search)) ||
                    (c.GSTIN != null && c.GSTIN.Contains(filter.Search)));

            if (filter.Type.HasValue) query = query.Where(c => c.CustomerType == filter.Type);
            if (filter.Status.HasValue) query = query.Where(c => c.Status == filter.Status);

            query = filter.SortBy switch
            {
                "CreatedAt" => filter.SortDesc
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => filter.SortDesc
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name)
            };

            var total = await query.CountAsync(ct);

            // Outstanding balance via invoice join
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CustomerListDto(
                    c.Id, c.Name, c.Email, c.Phone, c.CustomerType, c.Status,
                    c.GSTIN, c.BillingCity, c.BillingState,
                    c.Invoices
                        .Where(i => i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0)
                        .Sum(i => i.BalanceDue),
                    c.CreatedAt))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<CustomerListDto>>.Ok(
                new PagedResult<CustomerListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<CustomerDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (customer == null) return ApiResponse<CustomerDto>.Fail("Customer not found.");
            return ApiResponse<CustomerDto>.Ok(MapCustomer(customer));
        }

        public async Task<ApiResponse<CustomerDto>> CreateAsync(
            Guid tenantId, Guid userId, CreateCustomerDto dto, CancellationToken ct = default)
        {
            // GSTIN uniqueness check
            if (!string.IsNullOrEmpty(dto.GSTIN))
            {
                var isUnique = await _customers.IsGSTINUniqueAsync(tenantId, dto.GSTIN, ct: ct);
                if (!isUnique)
                    return ApiResponse<CustomerDto>.Fail("A customer with this GSTIN already exists.");
            }

            var customer = new Customer
            {
                TenantId = tenantId,
                Name = dto.Name?.Trim() ?? "",
                ContactPerson = dto.ContactPerson?.Trim(),
                Email = dto.Email?.Trim(),
                Phone = dto.Phone?.Trim(),
                AlternatePhone = dto.AlternatePhone?.Trim(),
                CustomerType = dto.CustomerType,
                Status = CustomerStatus.Active,
                GSTIN = dto.GSTIN?.Trim(),
                PAN = dto.PAN?.Trim(),
                IsGSTRegistered = !string.IsNullOrEmpty(dto.GSTIN?.Trim()),
                // Billing Address
                BillingAddressLine1 = dto.BillingAddress.Line1?.Trim(),
                BillingAddressLine2 = dto.BillingAddress.Line2?.Trim(),
                BillingCity = dto.BillingAddress.City?.Trim(),
                BillingState = dto.BillingAddress.State?.Trim(),
                BillingStateCode = dto.BillingAddress.StateCode?.Trim(),
                BillingPinCode = dto.BillingAddress.PinCode?.Trim(),
                BillingCountry = dto.BillingAddress.Country?.Trim() ?? "India",
                // Shipping Address
                ShippingSameAsBilling = dto.ShippingSameAsBilling,
                ShippingAddressLine1 = dto.ShippingSameAsBilling ? dto.BillingAddress.Line1?.Trim() : dto.ShippingAddress?.Line1?.Trim(),
                ShippingAddressLine2 = dto.ShippingSameAsBilling ? dto.BillingAddress.Line2?.Trim() : dto.ShippingAddress?.Line2?.Trim(),
                ShippingCity = dto.ShippingSameAsBilling ? dto.BillingAddress.City?.Trim() : dto.ShippingAddress?.City?.Trim(),
                ShippingState = dto.ShippingSameAsBilling ? dto.BillingAddress.State?.Trim() : dto.ShippingAddress?.State?.Trim(),
                ShippingStateCode = dto.ShippingSameAsBilling ? dto.BillingAddress.StateCode?.Trim() : dto.ShippingAddress?.StateCode?.Trim(),
                ShippingPinCode = dto.ShippingSameAsBilling ? dto.BillingAddress.PinCode?.Trim() : dto.ShippingAddress?.PinCode?.Trim(),
                CreditLimit = dto.CreditLimit,
                PaymentTermDays = dto.PaymentTermDays ?? 30,
                Notes = dto.Notes?.Trim(),
                Tags = dto.Tags?.Trim()
            };

            await _customers.AddAsync(customer, ct);
            await _uow.SaveChangesAsync(ct);

            await _audit.LogAsync(tenantId, userId, "Customer", customer.Id, "Create", ct: ct);
            _log.LogInformation("Customer created: {Name} for tenant {TenantId}", customer.Name, tenantId);

            return ApiResponse<CustomerDto>.Ok(MapCustomer(customer), "Customer created.");
        }

        public async Task<ApiResponse<CustomerDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdateCustomerDto dto, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (customer == null) return ApiResponse<CustomerDto>.Fail("Customer not found.");

            if (!string.IsNullOrEmpty(dto.GSTIN) && dto.GSTIN != customer.GSTIN)
            {
                var isUnique = await _customers.IsGSTINUniqueAsync(tenantId, dto.GSTIN, id, ct);
                if (!isUnique)
                    return ApiResponse<CustomerDto>.Fail("A customer with this GSTIN already exists.");
            }

            customer.Name = dto.Name?.Trim() ?? ""; customer.ContactPerson = dto.ContactPerson?.Trim();
            customer.Email = dto.Email?.Trim(); customer.Phone = dto.Phone?.Trim();
            customer.AlternatePhone = dto.AlternatePhone?.Trim();
            customer.CustomerType = dto.CustomerType; customer.Status = dto.Status;
            customer.GSTIN = dto.GSTIN?.Trim(); customer.PAN = dto.PAN?.Trim();
            customer.IsGSTRegistered = !string.IsNullOrEmpty(dto.GSTIN?.Trim());
            customer.BillingAddressLine1 = dto.BillingAddress.Line1?.Trim();
            customer.BillingAddressLine2 = dto.BillingAddress.Line2?.Trim();
            customer.BillingCity = dto.BillingAddress.City?.Trim();
            customer.BillingState = dto.BillingAddress.State?.Trim();
            customer.BillingStateCode = dto.BillingAddress.StateCode?.Trim();
            customer.BillingPinCode = dto.BillingAddress.PinCode?.Trim();
            customer.BillingCountry = dto.BillingAddress.Country?.Trim() ?? "India";
            customer.ShippingSameAsBilling = dto.ShippingSameAsBilling;

            if (!dto.ShippingSameAsBilling && dto.ShippingAddress != null)
            {
                customer.ShippingAddressLine1 = dto.ShippingAddress.Line1?.Trim();
                customer.ShippingAddressLine2 = dto.ShippingAddress.Line2?.Trim();
                customer.ShippingCity = dto.ShippingAddress.City?.Trim();
                customer.ShippingState = dto.ShippingAddress.State?.Trim();
                customer.ShippingStateCode = dto.ShippingAddress.StateCode?.Trim();
                customer.ShippingPinCode = dto.ShippingAddress.PinCode?.Trim();
            }

            customer.CreditLimit = dto.CreditLimit;
            customer.PaymentTermDays = dto.PaymentTermDays;
            customer.Notes = dto.Notes?.Trim(); customer.Tags = dto.Tags?.Trim();

            _customers.Update(customer);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<CustomerDto>.Ok(MapCustomer(customer), "Customer updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (customer == null) return ApiResponse<bool>.Fail("Customer not found.");

            var hasInvoices = await _invoices.AnyAsync(
                i => i.CustomerId == id && i.TenantId == tenantId, ct);
            if (hasInvoices)
                return ApiResponse<bool>.Fail(
                    "Cannot delete customer with existing invoices. Deactivate instead.");

            _customers.SoftDelete(customer);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Customer deleted.");
        }

        public async Task<ApiResponse<CustomerStatisticsDto>> GetStatisticsAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var customer = await _customers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (customer == null) return ApiResponse<CustomerStatisticsDto>.Fail("Customer not found.");

            var invoices = (await _invoices.GetByCustomerAsync(id, ct)).ToList();
            var activeInvoices = invoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();

            var totalRevenue = activeInvoices.Sum(i => i.GrandTotal);
            var outstanding = activeInvoices.Where(i => i.BalanceDue > 0).Sum(i => i.BalanceDue);
            var overdueDays = activeInvoices
                .Where(i => i.DueDate.HasValue && i.DueDate < DateTime.UtcNow && i.BalanceDue > 0)
                .Sum(i => (DateTime.UtcNow - i.DueDate!.Value).Days);

            return ApiResponse<CustomerStatisticsDto>.Ok(new CustomerStatisticsDto(
                id, customer.Name,
                activeInvoices.Count,
                totalRevenue,
                outstanding,
                activeInvoices.Count > 0 ? totalRevenue / activeInvoices.Count : 0,
                activeInvoices.MaxBy(i => i.InvoiceDate)?.InvoiceDate,
                overdueDays));
        }

        public async Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(
            Guid tenantId, string term, CancellationToken ct = default)
        {
            var customers = await _customers.SearchAsync(tenantId, term, ct);
            return ApiResponse<IEnumerable<SelectOptionDto>>.Ok(
                customers.Select(c => new SelectOptionDto(c.Id, c.Name, c.GSTIN)));
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static CustomerDto MapCustomer(Customer c) => new(
            c.Id, c.Name, c.ContactPerson, c.Email, c.Phone, c.AlternatePhone,
            c.CustomerType, c.Status, c.GSTIN, c.PAN, c.IsGSTRegistered,
            new AddressDto(c.BillingAddressLine1, c.BillingAddressLine2,
                c.BillingCity, c.BillingState, c.BillingStateCode,
                c.BillingPinCode, c.BillingCountry),
            c.ShippingSameAsBilling ? null : new AddressDto(
                c.ShippingAddressLine1, c.ShippingAddressLine2,
                c.ShippingCity, c.ShippingState, c.ShippingStateCode,
                c.ShippingPinCode, c.ShippingCountry),
            c.ShippingSameAsBilling, c.CreditLimit, c.PaymentTermDays,
            c.Notes, c.Tags, c.CreatedAt, c.UpdatedAt);
    }
}
