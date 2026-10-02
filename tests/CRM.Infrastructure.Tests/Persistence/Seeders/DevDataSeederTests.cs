using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Seeders;

public class DevDataSeederTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    public DevDataSeederTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_devdataseeder"))
            .Options;
    }

    private async Task<CrmDbContext> CreateResetContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        // Wipe in FK-safe order so re-running the test suite against the same
        // database is always a clean slate.
        context.InteractionLogs.RemoveRange(context.InteractionLogs);
        context.DonorApprovals.RemoveRange(context.DonorApprovals);
        context.DonorTasks.RemoveRange(context.DonorTasks);
        context.DonorOperationalRegions.RemoveRange(context.DonorOperationalRegions);
        context.DonorDonationTypes.RemoveRange(context.DonorDonationTypes);
        context.DonorContacts.RemoveRange(context.DonorContacts);
        context.DonorLegalAddresses.RemoveRange(context.DonorLegalAddresses);
        context.Donors.RemoveRange(context.Donors);
        context.UserRoles.RemoveRange(context.UserRoles);
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        await RoleSeeder.SeedAsync(context);
        await LookupSeeder.SeedAsync(context);

        var superAdminRole = await context.Roles.SingleAsync(r => r.Name == "SuperAdmin");
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "System",
            LastName = "Administrator",
            Email = "admin@crm.local",
            IsActive = true,
        };
        admin.PasswordHash = hasher.HashPassword(admin, "TestPassword123!");
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleId = superAdminRole.Id,
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = admin.Id,
        });
        await context.SaveChangesAsync();

        return context;
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesDonorsTasksApprovalsAndInteractions()
    {
        var context = await CreateResetContextAsync();

        await DevDataSeeder.SeedAsync(context);

        Assert.Equal(5, await context.Donors.CountAsync());
        Assert.Equal(4, await context.DonorTasks.CountAsync());
        Assert.Equal(3, await context.DonorApprovals.CountAsync());
        Assert.Equal(2, await context.InteractionLogs.CountAsync());
        Assert.True(await context.DonorLegalAddresses.CountAsync() >= 5);
        Assert.True(await context.DonorContacts.CountAsync() >= 5);
    }

    [Fact]
    public async Task SeedAsync_WhenDonorsAlreadyExist_IsANoOp()
    {
        var context = await CreateResetContextAsync();
        await DevDataSeeder.SeedAsync(context);

        var countAfterFirstRun = await context.Donors.CountAsync();

        await DevDataSeeder.SeedAsync(context);

        Assert.Equal(countAfterFirstRun, await context.Donors.CountAsync());
    }
}
