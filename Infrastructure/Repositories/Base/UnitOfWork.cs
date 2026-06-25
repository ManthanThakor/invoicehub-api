using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        AppDbContext db,
        IInvoiceRepository invoices,
        IPurchaseOrderRepository purchaseOrders,
        ICustomerRepository customers,
        ISupplierRepository suppliers,
        IProductRepository products,
        IPaymentRepository payments,
        IInventoryRepository inventory,
        IExpenseRepository expenses,
        IUserRepository users,
        ITenantRepository tenants,
        IAuditLogRepository auditLogs,
        IAIInsightRepository aiInsights,
        INotificationRepository notifications)
    {
        _db = db;
        Invoices = invoices;
        PurchaseOrders = purchaseOrders;
        Customers = customers;
        Suppliers = suppliers;
        Products = products;
        Payments = payments;
        Inventory = inventory;
        Expenses = expenses;
        Users = users;
        Tenants = tenants;
        AuditLogs = auditLogs;
        AIInsights = aiInsights;
        Notifications = notifications;
    }

    public IInvoiceRepository Invoices { get; }
    public IPurchaseOrderRepository PurchaseOrders { get; }
    public ICustomerRepository Customers { get; }
    public ISupplierRepository Suppliers { get; }
    public IProductRepository Products { get; }
    public IPaymentRepository Payments { get; }
    public IInventoryRepository Inventory { get; }
    public IExpenseRepository Expenses { get; }
    public IUserRepository Users { get; }
    public ITenantRepository Tenants { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IAIInsightRepository AIInsights { get; }
    public INotificationRepository Notifications { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null) return;
        _transaction = await _db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;

        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation, CancellationToken ct = default)
    {
        // No retry strategy — works cleanly without EnableRetryOnFailure
        _transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await operation();
            await _transaction.CommitAsync(ct);
        }
        catch
        {
            await _transaction.RollbackAsync(ct);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _db.Dispose();
    }
}