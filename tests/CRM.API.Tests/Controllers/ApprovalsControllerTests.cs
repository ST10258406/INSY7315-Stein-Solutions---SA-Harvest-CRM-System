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

public class ApprovalsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApprovalsControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForApprovalsTesting"));
            });
        });
    }

    private sealed record Fixture(HttpClient Client, Guid PendingApprovalId, Guid DonorId, Guid RelationshipManagerId, Guid ApprovedApprovalId);

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
            Email = $"test-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        var rm = new User { Id = Guid.NewGuid(), Email = "rm@example.com", FirstName = "Rita", LastName = "Manager", PasswordHash = "n/a" };
        var submitter = new User { Id = Guid.NewGuid(), Email = "submitter@example.com", FirstName = "Sam", LastName = "Submitter", PasswordHash = "n/a" };
        context.Users.AddRange(rm, submitter);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var donor = MakeDonor("FoodCorp SA", submitter.Id, rm.Id);
        var approvedDonor = MakeDonor("Already Co", submitter.Id, rm.Id);
        approvedDonor.Status = DonorStatus.Active;
        context.Donors.AddRange(donor, approvedDonor);

        var pending = new DonorApproval { Id = Guid.NewGuid(), DonorId = donor.Id, RequestedByUserId = submitter.Id, Status = ApprovalStatus.Pending };
        var approved = new DonorApproval { Id = Guid.NewGuid(), DonorId = approvedDonor.Id, RequestedByUserId = submitter.Id, Status = ApprovalStatus.Approved, ReviewedByUserId = loginUser.Id, ReviewedAt = DateTime.UtcNow };
        context.DonorApprovals.AddRange(pending, approved);

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, pending.Id, donor.Id, rm.Id, approved.Id);
    }

    private static Donor MakeDonor(string name, Guid creatorId, Guid rmId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = name,
        CompanyTypeId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        Status = DonorStatus.PendingReview,
        SubmissionSource = SubmissionSource.ManualCapture,
        RelationshipManagerId = rmId,
        CreatedByUserId = creatorId
    };

    private static JsonElement Body(HttpResponseMessage r) =>
        JsonDocument.Parse(r.Content.ReadAsStringAsync().Result).RootElement;

    // ---- GET /approvals -------------------------------------------------

    [Fact]
    public async Task GetApprovals_DefaultsToPending_WithNestedDonorAndSubmitter()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/approvals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = Body(response);
        Assert.Equal(1, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        var row = json.GetProperty("data")[0];
        Assert.Equal("Pending", row.GetProperty("status").GetString());
        Assert.Equal("FoodCorp SA", row.GetProperty("donor").GetProperty("companyName").GetString());
        Assert.Equal("Sam Submitter", row.GetProperty("requestedBy").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task GetApprovals_StatusFilter_ReturnsApprovedOnly()
    {
        var f = await SetupAsync("Admin");

        var json = Body(await f.Client.GetAsync("/api/v1/approvals?status=Approved"));

        Assert.Equal(1, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        Assert.Equal("Approved", json.GetProperty("data")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetApprovals_BadStatus_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/approvals?status=whenever");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approvals_ProcurementUser_Forbidden()
    {
        var f = await SetupAsync("Procurement");

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.GetAsync("/api/v1/approvals")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.PostAsync($"/api/v1/approvals/{f.PendingApprovalId}/approve", null)).StatusCode);
    }

    [Fact]
    public async Task GetApprovals_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/approvals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- approve ------------------------------------------------------

    [Fact]
    public async Task Approve_PendingApproval_Returns204_MovesApprovalAndDonor_NotifiesRm()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsync($"/api/v1/approvals/{f.PendingApprovalId}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var approval = await context.DonorApprovals.AsNoTracking().SingleAsync(a => a.Id == f.PendingApprovalId);
        var donor = await context.Donors.AsNoTracking().SingleAsync(d => d.Id == f.DonorId);
        Assert.Equal(ApprovalStatus.Approved, approval.Status);
        Assert.NotNull(approval.ReviewedAt);
        Assert.Equal(DonorStatus.Active, donor.Status);

        var notification = await context.Notifications.AsNoTracking().SingleAsync(n => n.UserId == f.RelationshipManagerId);
        Assert.Equal(NotificationType.DonorApproved, notification.NotificationType);
        Assert.Equal(f.DonorId, notification.RelatedEntityId);
    }

    [Fact]
    public async Task Approve_AlreadyApproved_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsync($"/api/v1/approvals/{f.ApprovedApprovalId}/approve", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approve_UnknownApproval_Returns404()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsync($"/api/v1/approvals/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- reject ------------------------------------------------------

    [Fact]
    public async Task Reject_PendingApproval_Returns204_MovesToRejected_ReasonInNotification()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsJsonAsync(
            $"/api/v1/approvals/{f.PendingApprovalId}/reject",
            new { rejectionReason = "Failed food-safety vetting." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var approval = await context.DonorApprovals.AsNoTracking().SingleAsync(a => a.Id == f.PendingApprovalId);
        var donor = await context.Donors.AsNoTracking().SingleAsync(d => d.Id == f.DonorId);
        Assert.Equal(ApprovalStatus.Rejected, approval.Status);
        Assert.Equal("Failed food-safety vetting.", approval.RejectionReason);
        Assert.Equal(DonorStatus.Rejected, donor.Status);

        var notification = await context.Notifications.AsNoTracking().SingleAsync(n => n.UserId == f.RelationshipManagerId);
        Assert.Equal(NotificationType.DonorRejected, notification.NotificationType);
        Assert.Contains("Failed food-safety vetting.", notification.Message);
    }

    [Fact]
    public async Task Reject_MissingReason_Returns400_AndDoesNotChangeState()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsJsonAsync($"/api/v1/approvals/{f.PendingApprovalId}/reject", new { rejectionReason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var approval = await context.DonorApprovals.AsNoTracking().SingleAsync(a => a.Id == f.PendingApprovalId);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);
    }

    [Fact]
    public async Task Reject_AlreadyReviewed_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsJsonAsync($"/api/v1/approvals/{f.ApprovedApprovalId}/reject", new { rejectionReason = "too late" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reject_UnknownApproval_Returns404()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.PostAsJsonAsync($"/api/v1/approvals/{Guid.NewGuid()}/reject", new { rejectionReason = "nope" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
