namespace CRM.Infrastructure.Persistence.Seeders;

using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

/// <summary>
/// Seeds a single default SuperAdmin user on first run. Password is never
/// hardcoded; it comes from configuration, which reads from .env locally
/// and Azure Key Vault in production — this class doesn't need to know which.
/// </summary>
public static class AdminUserSeeder
{
    public static async Task SeedAsync(CrmDbContext context, IConfiguration configuration, bool isDevelopment = true)
    {
        if (await context.Users.AnyAsync())
        {
            // Already seeded — idempotent guard (safe here: this seeder only ever creates ONE
            // specific user, unlike RoleSeeder's multi-row case, so a top-level guard is correct).
            // Never touches an existing admin's password; only repairs the legacy bootstrap admin.
            await UpgradeLegacyAdminAsync(context, configuration, isDevelopment);
            return;
        }

        var password = configuration["ADMIN_DEFAULT_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "ADMIN_DEFAULT_PASSWORD is not set. Add it to .env (local dev) " +
                "or Azure Key Vault (production) before starting the application.");
        }

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "System",
            LastName = "Administrator",
            Email = AdminSeedGuard.ResolveAdminEmail(configuration, isDevelopment),
            IsActive = true,
            // Seeded credentials are provisioning-only: the first login must replace them.
            MustChangePassword = true,
        };

        var hasher = new PasswordHasher<User>();
        adminUser.PasswordHash = hasher.HashPassword(adminUser, password);

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        var superAdminRole = await context.Roles.SingleAsync(r => r.Name == "SuperAdmin");

        context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = superAdminRole.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = adminUser.Id, // self-assigned at bootstrap — no other user exists yet
        });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Databases seeded before ADMIN_EMAIL / must_change_password existed hold the bootstrap
    /// SuperAdmin as the non-routable admin@crm.local on its original password. Once a real
    /// ADMIN_EMAIL is configured, move that account onto it (so password recovery works) and
    /// force a password change. Runs only while the legacy address still exists, so it fires
    /// once and never overwrites a password. Skipped when the configured address is the legacy
    /// default (local development) or already belongs to another user.
    /// </summary>
    private static async Task UpgradeLegacyAdminAsync(CrmDbContext context, IConfiguration configuration, bool isDevelopment)
    {
        var configuredEmail = AdminSeedGuard.ResolveAdminEmail(configuration, isDevelopment);
        if (string.Equals(configuredEmail, AdminSeedGuard.DevelopmentDefaultEmail, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var legacyAdmin = await context.Users.SingleOrDefaultAsync(u =>
            u.Email == AdminSeedGuard.DevelopmentDefaultEmail
            && u.UserRoles.Any(ur => ur.Role.Name == "SuperAdmin"));
        if (legacyAdmin is null)
        {
            return;
        }

        if (await context.Users.AnyAsync(u => u.Email == configuredEmail))
        {
            return; // never create a duplicate address; an operator has to resolve this by hand
        }

        legacyAdmin.Email = configuredEmail;
        legacyAdmin.MustChangePassword = true;
        await context.SaveChangesAsync();
    }
}
