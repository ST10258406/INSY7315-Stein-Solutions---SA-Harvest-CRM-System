namespace CRM.Infrastructure.Tests;

using CRM.Domain.Common;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

public class UpdatedAtInterceptorTests
{
    private const string TestConnectionString = "Host=localhost;Database=crm_test;Username=postgres;Password=P@ss1234ID";

    [Fact]
    public async Task SavingChanges_SetsUpdatedAt_OnModifiedEntity()
    {
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestConnectionString)
            .AddInterceptors(new UpdatedAtInterceptor())
            .Options;

        using var context = new CrmDbContext(options);

        // Ensure database exists and seed data if not present,
        // since there's no pre-existing DB pattern for tests.
        // (This connects to the same local Postgres container, but a
        // separate crm_test database — never touches the real crmdb dev data.)
        await context.Database.EnsureCreatedAsync();

        if (!await context.Roles.AnyAsync())
        {
            context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "Initial Role" });
            await context.SaveChangesAsync();
        }

        var role = await context.Roles.FirstAsync();
        var originalUpdatedAt = role.UpdatedAt;

        role.Description = $"Changed for test {Guid.NewGuid()}";
        await Task.Delay(10); // ensure the clock actually advances between reads
        await context.SaveChangesAsync();

        Assert.True(role.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void SavingChanges_DoesNotSetUpdatedAt_OnInteractionLog()
    {
        // InteractionLog has no UpdatedAt property at all — this test should
        // fail to compile if someone ever adds one, which is itself a useful signal.
        // Confirm instead that InteractionLog does not implement IHasUpdatedAt:
        Assert.False(typeof(IHasUpdatedAt).IsAssignableFrom(typeof(InteractionLog)));
        Assert.False(typeof(IHasUpdatedAt).IsAssignableFrom(typeof(AuditLog)));
    }
}