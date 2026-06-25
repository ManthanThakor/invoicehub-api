using Application.DTOs;
using Application.Services.System;
using Core.Entities;
using Core.Enums;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Purchases
{

    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _suppliers;
        private readonly IPurchaseOrderRepository _pos;
        private readonly IAuditService _audit;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SupplierService> _log;

        public SupplierService(
            ISupplierRepository suppliers, IPurchaseOrderRepository pos,
            IAuditService audit, IUnitOfWork uow, ILogger<SupplierService> log)
        {
            _suppliers = suppliers; _pos = pos;
            _audit = audit; _uow = uow; _log = log;
        }

        public async Task<ApiResponse<PagedResult<SupplierListDto>>> GetListAsync(
            Guid tenantId, SupplierFilterDto filter, CancellationToken ct = default)
        {
            var query = _suppliers.Query(tenantId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(s =>
                    s.Name.Contains(filter.Search) ||
                    (s.Email != null && s.Email.Contains(filter.Search)) ||
                    (s.GSTIN != null && s.GSTIN.Contains(filter.Search)));

            if (filter.Status.HasValue) query = query.Where(s => s.Status == filter.Status);

            query = filter.SortDesc
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new SupplierListDto(
                    s.Id, s.Name, s.Email, s.Phone, s.Status, s.GSTIN, s.City))
                .ToListAsync(ct);

            return ApiResponse<PagedResult<SupplierListDto>>.Ok(
                new PagedResult<SupplierListDto>(items, total, filter.Page, filter.PageSize));
        }

        public async Task<ApiResponse<SupplierDto>> GetAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var supplier = await _suppliers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (supplier == null) return ApiResponse<SupplierDto>.Fail("Supplier not found.");
            return ApiResponse<SupplierDto>.Ok(MapSupplier(supplier));
        }

        public async Task<ApiResponse<SupplierDto>> CreateAsync(
            Guid tenantId, Guid userId, CreateSupplierDto dto, CancellationToken ct = default)
        {
            var supplier = new Supplier
            {
                TenantId = tenantId,
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                Email = dto.Email,
                Phone = dto.Phone,
                GSTIN = dto.GSTIN,
                PAN = dto.PAN,
                Status = SupplierStatus.Active,
                AddressLine1 = dto.Address.Line1,
                AddressLine2 = dto.Address.Line2,
                City = dto.Address.City,
                State = dto.Address.State,
                StateCode = dto.Address.StateCode,
                PinCode = dto.Address.PinCode,
                Country = dto.Address.Country ?? "India",
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankIFSC = dto.BankIFSC,
                PaymentTermDays = dto.PaymentTermDays ?? 30,
                Notes = dto.Notes
            };

            await _suppliers.AddAsync(supplier, ct);
            await _uow.SaveChangesAsync(ct);

            await _audit.LogAsync(tenantId, userId, "Supplier", supplier.Id, "Create", ct: ct);
            _log.LogInformation("Supplier created: {Name} for tenant {TenantId}", supplier.Name, tenantId);

            return ApiResponse<SupplierDto>.Ok(MapSupplier(supplier), "Supplier created.");
        }

        public async Task<ApiResponse<SupplierDto>> UpdateAsync(
            Guid tenantId, Guid id, UpdateSupplierDto dto, CancellationToken ct = default)
        {
            var supplier = await _suppliers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (supplier == null) return ApiResponse<SupplierDto>.Fail("Supplier not found.");

            supplier.Name = dto.Name; supplier.ContactPerson = dto.ContactPerson;
            supplier.Email = dto.Email; supplier.Phone = dto.Phone;
            supplier.GSTIN = dto.GSTIN; supplier.PAN = dto.PAN;
            supplier.Status = dto.Status;
            supplier.AddressLine1 = dto.Address.Line1;
            supplier.AddressLine2 = dto.Address.Line2;
            supplier.City = dto.Address.City; supplier.State = dto.Address.State;
            supplier.StateCode = dto.Address.StateCode; supplier.PinCode = dto.Address.PinCode;
            supplier.Country = dto.Address.Country ?? "India";
            supplier.BankName = dto.BankName;
            supplier.BankAccountNumber = dto.BankAccountNumber;
            supplier.BankIFSC = dto.BankIFSC;
            supplier.PaymentTermDays = dto.PaymentTermDays;
            supplier.Notes = dto.Notes;

            _suppliers.Update(supplier);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<SupplierDto>.Ok(MapSupplier(supplier), "Supplier updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(
            Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var supplier = await _suppliers.GetByIdWithTenantAsync(id, tenantId, ct);
            if (supplier == null) return ApiResponse<bool>.Fail("Supplier not found.");

            var hasPOs = await _pos.AnyAsync(p => p.SupplierId == id && p.TenantId == tenantId, ct);
            if (hasPOs)
                return ApiResponse<bool>.Fail(
                    "Cannot delete supplier with purchase orders. Deactivate instead.");

            _suppliers.SoftDelete(supplier);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "Supplier deleted.");
        }

        public async Task<ApiResponse<IEnumerable<SelectOptionDto>>> SearchAsync(
            Guid tenantId, string term, CancellationToken ct = default)
        {
            var suppliers = await _suppliers.SearchAsync(tenantId, term, ct);
            return ApiResponse<IEnumerable<SelectOptionDto>>.Ok(
                suppliers.Select(s => new SelectOptionDto(s.Id, s.Name, s.GSTIN)));
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static SupplierDto MapSupplier(Supplier s) => new(
            s.Id, s.Name, s.ContactPerson, s.Email, s.Phone,
            s.GSTIN, s.PAN, s.Status,
            new AddressDto(s.AddressLine1, s.AddressLine2, s.City,
                s.State, s.StateCode, s.PinCode, s.Country),
            s.BankName, s.BankAccountNumber, s.BankIFSC,
            s.PaymentTermDays, s.Notes, s.CreatedAt);
    }
}
