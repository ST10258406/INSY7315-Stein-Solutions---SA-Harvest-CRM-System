namespace CRM.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time-only factory used exclusively by `dotnet ef` CLI commands
/// (migrations add/remove, database update) to construct CrmDbContext
/// outside of the app's normal DI pipeline. Never invoked at runtime —
/// the real app always goes through AddInfrastructureServices() instead.
/// The connection string here does not need to point at a real, reachable
/// database — generating a migration never actually connects to it.
/// </summary>
public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    public CrmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CrmDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=crmdb;Username=postgres;Password=P@ss1234ID");

        return new CrmDbContext(optionsBuilder.Options);
    }
}