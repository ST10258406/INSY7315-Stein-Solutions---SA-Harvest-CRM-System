namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class DashboardControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(
                    d => d.ServiceType.Namespace != null &&
                         (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                          d.ServiceType.Namespace.StartsWith("Npgsql.EntityFrameworkCore.PostgreSQL"))).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForDashboardTesting"));
            });
        });
    }

    private sealed record Fixture(HttpClient Client, Guid CurrentUserId, Guid OtherUserId);

    private async Task<Fixture> SetupAsync(string roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        context.Roles.Add(role);

        const string password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var loginUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"dashboard-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        var otherUser = new User { Id = Guid.NewGuid(), Email = "dashboard-other@example.com", FirstName = "Other", LastName = "User", PasswordHash = "n/a" };
        context.Users.Add(otherUser);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, loginUser.Id, otherUser.Id);
    }

    private static Donor MakeDonor(string name, Guid creatorId, DonorStatus status = DonorStatus.Active, Guid? rmId = null, DateTime? followUpDate = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = name,
        CompanyTypeId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        Status = status,
        SubmissionSource = SubmissionSource.ManualCapture,
        RelationshipManagerId = rmId,
        FollowUpDate = followUpDate,
        CreatedByUserId = creatorId
    };

    private static InteractionLog MakeLog(Guid donorId, Guid createdByUserId, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        CreatedByUserId = createdByUserId,
        InteractionType = InteractionType.Note,
        Subject = "Subject",
        Body = "Body",
        CreatedAt = createdAt
    };

    private static JsonElement Body(HttpResponseMessage r) =>
        JsonDocument.Parse(r.Content.ReadAsStringAsync().Result).RootElement;

    [Fact]
    public async Task GetStats_Admin_ReturnsAllKpisIncludingRealPendingApprovals()
    {
        var f = await SetupAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var activeDonor1 = MakeDonor("Active Co 1", f.CurrentUserId, DonorStatus.Active);
        var activeDonor2 = MakeDonor("Active Co 2", f.CurrentUserId, DonorStatus.Active);
        var pendingDonor = MakeDonor("Pending Co", f.CurrentUserId, DonorStatus.PendingReview);
        context.Donors.AddRange(activeDonor1, activeDonor2, pendingDonor);
        await context.SaveChangesAsync();

        context.DonorApprovals.Add(new DonorApproval { Id = Guid.NewGuid(), DonorId = pendingDonor.Id, RequestedByUserId = f.CurrentUserId, Status = ApprovalStatus.Pending });

        context.DonorTasks.AddRange(
            new DonorTask { Id = Guid.NewGuid(), DonorId = activeDonor1.Id, Title = "Call", DueDate = DateTime.UtcNow.AddDays(1), IsCompleted = false, AssignedToUserId = f.CurrentUserId, CreatedByUserId = f.CurrentUserId },
            new DonorTask { Id = Guid.NewGuid(), DonorId = activeDonor1.Id, Title = "Follow up", DueDate = DateTime.UtcNow.AddDays(2), IsCompleted = true, AssignedToUserId = f.CurrentUserId, CreatedByUserId = f.CurrentUserId, CompletedByUserId = f.CurrentUserId });

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 15, 9, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1).AddDays(1);
        context.InteractionLogs.AddRange(
            MakeLog(activeDonor1.Id, f.CurrentUserId, thisMonth),
            MakeLog(activeDonor2.Id, f.CurrentUserId, thisMonth),
            MakeLog(pendingDonor.Id, f.CurrentUserId, lastMonth));

        await context.SaveChangesAsync();

        var response = await f.Client.GetAsync("/api/v1/dashboard/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");

        Assert.Equal(3, data.GetProperty("totalDonors").GetInt32());
        Assert.Equal(2, data.GetProperty("activeDonors").GetInt32());
        Assert.Equal(1, data.GetProperty("pendingApprovals").GetInt32());
        Assert.Equal(1, data.GetProperty("myOpenTasks").GetInt32());
        Assert.Equal(2, data.GetProperty("donorsContactedThisMonth").GetInt32());
        Assert.Equal(1, data.GetProperty("donorsContactedLastMonth").GetInt32());
    }

    [Fact]
    public async Task GetStats_ProcurementUser_PendingApprovalsIsForcedToZero_DespitePendingApprovalsExisting()
    {
        var f = await SetupAsync("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var donor = MakeDonor("Pending Co", f.CurrentUserId, DonorStatus.PendingReview);
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        context.DonorApprovals.Add(new DonorApproval { Id = Guid.NewGuid(), DonorId = donor.Id, RequestedByUserId = f.CurrentUserId, Status = ApprovalStatus.Pending });
        await context.SaveChangesAsync();

        var response = await f.Client.GetAsync("/api/v1/dashboard/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");
        Assert.Equal(0, data.GetProperty("pendingApprovals").GetInt32());
    }

    [Fact]
    public async Task GetStats_ScopesMyOpenTasksAndOverdueFollowUps_ToTheCallingUserOnly()
    {
        var f = await SetupAsync("Marketing");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var myDonor = MakeDonor("My Donor", f.CurrentUserId, rmId: f.CurrentUserId, followUpDate: DateTime.UtcNow.AddDays(-1));
        var otherDonor = MakeDonor("Other Donor", f.CurrentUserId, rmId: f.OtherUserId, followUpDate: DateTime.UtcNow.AddDays(-1));
        context.Donors.AddRange(myDonor, otherDonor);
        await context.SaveChangesAsync();

        context.DonorTasks.AddRange(
            new DonorTask { Id = Guid.NewGuid(), DonorId = myDonor.Id, Title = "Mine", DueDate = DateTime.UtcNow.AddDays(1), IsCompleted = false, AssignedToUserId = f.CurrentUserId, CreatedByUserId = f.CurrentUserId },
            new DonorTask { Id = Guid.NewGuid(), DonorId = otherDonor.Id, Title = "Not mine", DueDate = DateTime.UtcNow.AddDays(1), IsCompleted = false, AssignedToUserId = f.OtherUserId, CreatedByUserId = f.CurrentUserId });
        await context.SaveChangesAsync();

        var response = await f.Client.GetAsync("/api/v1/dashboard/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");
        Assert.Equal(1, data.GetProperty("myOpenTasks").GetInt32());
        Assert.Equal(1, data.GetProperty("myOverdueFollowUps").GetInt32());
    }

    [Fact]
    public async Task GetStats_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/dashboard/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
