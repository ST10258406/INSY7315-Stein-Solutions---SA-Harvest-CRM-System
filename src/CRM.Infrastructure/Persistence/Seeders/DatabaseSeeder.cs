namespace CRM.Infrastructure.Persistence.Seeders;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

public static class DatabaseSeeder
{
    /// <summary>
    /// Runs all seeders in dependency order: Roles → Lookups → AdminUser.
    /// AdminUserSeeder needs the SuperAdmin role to already exist. Each individual
    /// seeder has its own idempotent guard, so calling this on every startup is safe.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await RoleSeeder.SeedAsync(context);
        await LookupSeeder.SeedAsync(context);
        await AdminUserSeeder.SeedAsync(context, configuration);
    }
}
