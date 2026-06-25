using Core.Common;

namespace Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IInvoiceRepository Invoices { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    ICustomerRepository Customers { get; }
    ISupplierRepository Suppliers { get; }
    IProductRepository Products { get; }
    IPaymentRepository Payments { get; }
    IInventoryRepository Inventory { get; }
    IExpenseRepository Expenses { get; }
    IUserRepository Users { get; }
    ITenantRepository Tenants { get; }
    IAuditLogRepository AuditLogs { get; }
    IAIInsightRepository AIInsights { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
}