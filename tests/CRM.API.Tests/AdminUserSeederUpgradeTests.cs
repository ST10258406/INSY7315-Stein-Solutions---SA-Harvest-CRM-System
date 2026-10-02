namespace CRM.API.Tests;

using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Upgrade path for databases seeded before ADMIN_EMAIL / must_change_password existed: the
/// bootstrap SuperAdmin is moved onto the configured real address and forced to change password.
/// </summary>
public class AdminUserSeederUpgradeTests
{
    private const string LegacyEmail = "admin@crm.local";
    private const string RealEmail = "ops@saharvest.org.za";

    private static CrmDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CrmDbContext>()
            .UseInMemoryDatabase($"AdminUpgrade-{Guid.NewGuid():N}")
            .Options);

    private static IConfiguration Config(string? adminEmail) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_EMAIL"] = adminEmail,
                ["ADMIN_DEFAULT_PASSWORD"] = "k9#Vq2!xLm7@Rt4w"
            })
            .Build();

    private static async Task<User> SeedLegacyAdminAsync(CrmDbContext context, string email = LegacyEmail, string role = "SuperAdmin")
    {
        var roleEntity = new Role { Id = Guid.NewGuid(), Name = role };
        var user = new User { Id = Guid.NewGuid(), Email = email, FirstName = "System", LastName = "Administrator", PasswordHash = "hash", IsActive = true };
        context.Roles.Add(roleEntity);
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleEntity.Id, AssignedByUserId = user.Id });
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LegacyAdmin_IsMovedToTheConfiguredEmail_AndForcedToChangePassword()
    {
        using var context = NewContext();
        var admin = await SeedLegacyAdminAsync(context);

        await AdminUserSeeder.SeedAsync(context, Config(RealEmail), isDevelopment: false);

        var reloaded = await context.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal(RealEmail, reloaded.Email);
        Assert.True(reloaded.MustChangePassword);
        Assert.Equal("hash", reloaded.PasswordHash); // never touches the password
    }

    [Fact]
    public async Task SecondRun_DoesNotReFlagAnAdminWhoAlreadyChangedPassword()
    {
        using var context = NewContext();
        var admin = await SeedLegacyAdminAsync(context);
        await AdminUserSeeder.SeedAsync(context, Config(RealEmail), isDevelopment: false);

        admin = await context.Users.SingleAsync(u => u.Id == admin.Id);
        admin.MustChangePassword = false; // they changed it
        await context.SaveChangesAsync();

        await AdminUserSeeder.SeedAsync(context, Config(RealEmail), isDevelopment: false);

        Assert.False((await context.Users.SingleAsync(u => u.Id == admin.Id)).MustChangePassword);
    }

    [Fact]
    public async Task ConfiguredEmailAlreadyTakenByAnotherUser_LeavesTheLegacyAdminUntouched()
    {
        using var context = NewContext();
        var admin = await SeedLegacyAdminAsync(context);
        context.Users.Add(new User { Id = Guid.NewGuid(), Email = RealEmail, FirstName = "Other", LastName = "User", PasswordHash = "x", IsActive = true });
        await context.SaveChangesAsync();

        await AdminUserSeeder.SeedAsync(context, Config(RealEmail), isDevelopment: false);

        var reloaded = await context.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal(LegacyEmail, reloaded.Email);
        Assert.False(reloaded.MustChangePassword);
    }

    [Fact]
    public async Task Development_WithDefaultEmail_ChangesNothing()
    {
        using var context = NewContext();
        var admin = await SeedLegacyAdminAsync(context);

        await AdminUserSeeder.SeedAsync(context, Config(null), isDevelopment: true);

        var reloaded = await context.Users.SingleAsync(u => u.Id == admin.Id);
        Assert.Equal(LegacyEmail, reloaded.Email);
        Assert.False(reloaded.MustChangePassword);
    }

    [Fact]
    public async Task OnlyASuperAdminAtTheLegacyAddressIsUpgraded()
    {
        using var context = NewContext();
        var notAdmin = await SeedLegacyAdminAsync(context, role: "Marketing");

        await AdminUserSeeder.SeedAsync(context, Config(RealEmail), isDevelopment: false);

        Assert.Equal(LegacyEmail, (await context.Users.SingleAsync(u => u.Id == notAdmin.Id)).Email);
    }
}
