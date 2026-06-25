using Core.Common;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AIInsight> AIInsights => Set<AIInsight>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global soft-delete query filters
        mb.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ProductCategory>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<InvoiceItem>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<PurchaseOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<CreditNote>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<InventoryMovement>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<AIInsight>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            // Normalize all DateTime properties to UTC (required by Npgsql timestamptz)
            foreach (var prop in entry.Properties)
            {
                if (prop.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                    prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                else if (prop.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                    prop.CurrentValue = dto.ToUniversalTime();
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
