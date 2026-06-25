using Core.Entities;
using Core.Enums;
using Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace InvoiceHub.Infrastructure.Data;

public static class SuperAdminSeeder
{
    /// <summary>
    /// Seeds the platform SuperAdmin if one does not already exist.
    /// Call this once from Program.cs after migrations.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config       = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var log          = scope.ServiceProvider
                               .GetRequiredService<ILogger<AppDbContext>>();

        // ── Read config ───────────────────────────────────────────────
        var email     = config["SuperAdmin:Email"]     ?? "superadmin@invoicehub.in";
        var password  = config["SuperAdmin:Password"]  ?? "superadmin123";
        var firstName = config["SuperAdmin:FirstName"] ?? "Super";
        var lastName  = config["SuperAdmin:LastName"]  ?? "Admin";

        // ── Already seeded? ───────────────────────────────────────────
        var exists = await db.Users
            .AnyAsync(u => u.Role == UserRole.SuperAdmin);

        if (exists)
        {
            log.LogInformation("SuperAdmin already exists — skipping seed.");
            return;
        }

        // ── Create platform tenant (InvoiceHub itself) ────────────────
        var platformTenant = new Tenant
        {
            BusinessName    = "InvoiceHub Platform",
            Email           = email,
            IsGSTRegistered = false,
            IsActive        = true
        };

        // ── Create SuperAdmin user ────────────────────────────────────
        var superAdmin = new User
        {
            FirstName     = firstName,
            LastName      = lastName,
            Email         = email.ToLowerInvariant(),
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            Role          = UserRole.SuperAdmin,
            Status        = UserStatus.Active,
            EmailVerified = true,
            Tenant        = platformTenant
        };

        await db.Tenants.AddAsync(platformTenant);
        await db.Users.AddAsync(superAdmin);
        await db.SaveChangesAsync();

        log.LogWarning(
            "SuperAdmin created: {Email}. " +
            "IMPORTANT: Change the default password immediately!",
            email);
    }
}