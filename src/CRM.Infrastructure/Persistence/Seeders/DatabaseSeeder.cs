namespace CRM.Infrastructure.Persistence.Seeders;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public static class DatabaseSeeder
{
    /// <summary>
    /// Runs all seeders in dependency order: Roles → Lookups → donor reference
    /// number backfill → AdminUser → SystemUser → (Development only) dev
    /// convenience data. AdminUserSeeder needs the SuperAdmin role to already
    /// exist. SystemUserSeeder MUST run after AdminUserSeeder, not before —
    /// AdminUserSeeder's own idempotency guard is "does any user exist at all",
    /// so seeding the system user first would make it wrongly skip creating the
    /// SuperAdmin account. DevDataSeeder needs the admin user and lookups to
    /// already exist. The reference number backfill only touches pre-existing
    /// donor rows (see its own remarks), so its position relative to the others
    /// doesn't matter beyond running once schema migrations have applied. Each
    /// individual seeder has its own idempotent guard, so calling this on every
    /// startup is safe.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, bool isDevelopment)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Fail fast, before touching any data, if the admin seed settings are unsafe.
        AdminSeedGuard.EnsureSafe(configuration, isDevelopment);

        await RoleSeeder.SeedAsync(context);
        await LookupSeeder.SeedAsync(context);
        await DonorReferenceNumberBackfillSeeder.SeedAsync(context);
        await AdminUserSeeder.SeedAsync(context, configuration, isDevelopment);
        await SystemUserSeeder.SeedAsync(context);

        // Fake donors/tasks/approvals — never seeded outside local Development,
        // so Staging/Production never gets test data mixed in with real donors.
        if (isDevelopment)
        {
            await DevDataSeeder.SeedAsync(context);
        }
    }
}
