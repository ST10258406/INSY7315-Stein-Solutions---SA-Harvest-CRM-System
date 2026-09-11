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

public class ReportsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportsControllerTests(WebApplicationFactory<Program> factory)
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForReportsTesting"));
            });
        });
    }

    private sealed record Fixture(HttpClient Client, Guid ManagerAId, Guid ManagerBId);

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

        var managerA = new User { Id = Guid.NewGuid(), Email = "jane.doe@example.com", FirstName = "Jane", LastName = "Doe", PasswordHash = "n/a" };
        var managerB = new User { Id = Guid.NewGuid(), Email = "john.smith@example.com", FirstName = "John", LastName = "Smith", PasswordHash = "n/a" };
        context.Users.AddRange(managerA, managerB);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var donorForA = MakeDonor("Donor For A", loginUser.Id, managerA.Id);
        var otherDonorForA = MakeDonor("Other Donor For A", loginUser.Id, managerA.Id);
        var donorForB = MakeDonor("Donor For B", loginUser.Id, managerB.Id);
        var unassignedDonor = MakeDonor("Unassigned Donor", loginUser.Id, null);
        context.Donors.AddRange(donorForA, otherDonorForA, donorForB, unassignedDonor);

        var inRangeDay = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        context.InteractionLogs.AddRange(
            // donorForA gets 3 interactions in-period -> should count once, not thrice.
            MakeLog(donorForA.Id, loginUser.Id, inRangeDay),
            MakeLog(donorForA.Id, loginUser.Id, inRangeDay.AddHours(1)),
            MakeLog(donorForA.Id, loginUser.Id, inRangeDay.AddHours(2)),
            MakeLog(otherDonorForA.Id, loginUser.Id, inRangeDay.AddHours(3)),
            MakeLog(donorForB.Id, loginUser.Id, inRangeDay.AddHours(4)),
            MakeLog(unassignedDonor.Id, loginUser.Id, inRangeDay.AddHours(5)),
            // Outside the [2026-07-01, 2026-07-31] period used by the tests below.
            MakeLog(donorForA.Id, loginUser.Id, new DateTime(2026, 8, 1, 0, 0, 1, DateTimeKind.Utc)));

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, managerA.Id, managerB.Id);
    }

    private static Donor MakeDonor(string name, Guid creatorId, Guid? rmId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = name,
        CompanyTypeId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        Status = DonorStatus.Active,
        SubmissionSource = SubmissionSource.ManualCapture,
        RelationshipManagerId = rmId,
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
    public async Task GetDonorsContacted_ReturnsTotalsAndPerManagerBreakdown()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?startDate=2026-07-01&endDate=2026-07-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");

        Assert.Equal("2026-07-01", data.GetProperty("period").GetProperty("startDate").GetString());
        Assert.Equal("2026-07-31", data.GetProperty("period").GetProperty("endDate").GetString());

        // 4 distinct donors contacted: donorForA, otherDonorForA, donorForB, unassignedDonor.
        Assert.Equal(4, data.GetProperty("totalDonorsContacted").GetInt32());

        var byManager = data.GetProperty("byManager").EnumerateArray().ToList();
        Assert.Equal(2, byManager.Count);

        var rowA = byManager.Single(m => m.GetProperty("manager").GetProperty("id").GetGuid() == f.ManagerAId);
        Assert.Equal("Jane Doe", rowA.GetProperty("manager").GetProperty("fullName").GetString());
        Assert.Equal(2, rowA.GetProperty("donorsContacted").GetInt32());
        Assert.Equal(4, rowA.GetProperty("totalInteractions").GetInt32());

        var rowB = byManager.Single(m => m.GetProperty("manager").GetProperty("id").GetGuid() == f.ManagerBId);
        Assert.Equal(1, rowB.GetProperty("donorsContacted").GetInt32());
        Assert.Equal(1, rowB.GetProperty("totalInteractions").GetInt32());
    }

    [Fact]
    public async Task GetDonorsContacted_RelationshipManagerFilter_ScopesToOneManager()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync(
            $"/api/v1/reports/donors-contacted?startDate=2026-07-01&endDate=2026-07-31&relationshipManagerId={f.ManagerAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");

        Assert.Equal(2, data.GetProperty("totalDonorsContacted").GetInt32());
        var byManager = data.GetProperty("byManager").EnumerateArray().ToList();
        var row = Assert.Single(byManager);
        Assert.Equal(f.ManagerAId, row.GetProperty("manager").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetDonorsContacted_ExcludesInteractionsOutsideRequestedPeriod()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?startDate=2026-08-01&endDate=2026-08-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");

        Assert.Equal(1, data.GetProperty("totalDonorsContacted").GetInt32());
    }

    [Fact]
    public async Task GetDonorsContacted_MissingStartDate_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?endDate=2026-07-31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsContacted_MissingEndDate_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?startDate=2026-07-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsContacted_StartDateAfterEndDate_Returns400()
    {
        var f = await SetupAsync("Admin");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?startDate=2026-07-31&endDate=2026-07-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsContacted_NonAdminUser_Forbidden()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.GetAsync("/api/v1/reports/donors-contacted?startDate=2026-07-01&endDate=2026-07-31");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsContacted_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient()
            .GetAsync("/api/v1/reports/donors-contacted?startDate=2026-07-01&endDate=2026-07-31");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> SetupForRegionsAsync(string roleName)
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
            Email = $"test-regions-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var jhb = new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true, SortOrder = 1 };
        var cpt = new LookupOperationalRegion { Id = 2, Code = "CPT", Name = "Cape Town", IsActive = true, SortOrder = 2 };
        context.LookupOperationalRegions.AddRange(jhb, cpt);

        var donorInJhb = MakeDonor("Donor In JHB", loginUser.Id, null);
        var otherDonorInJhb = MakeDonor("Other Donor In JHB", loginUser.Id, null);
        context.Donors.AddRange(donorInJhb, otherDonorInJhb);
        await context.SaveChangesAsync();

        context.DonorOperationalRegions.Add(new DonorOperationalRegion { DonorId = donorInJhb.Id, OperationalRegionId = jhb.Id });
        context.DonorOperationalRegions.Add(new DonorOperationalRegion { DonorId = otherDonorInJhb.Id, OperationalRegionId = jhb.Id });
        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    [Fact]
    public async Task GetDonorsByRegion_ReturnsCountsIncludingZeroForEmptyRegions()
    {
        var client = await SetupForRegionsAsync("Admin");

        var response = await client.GetAsync("/api/v1/reports/donors-by-region");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data").EnumerateArray().ToList();

        Assert.Equal(2, data.Count);

        var jhb = data.Single(r => r.GetProperty("region").GetString() == "JHB");
        Assert.Equal("Johannesburg", jhb.GetProperty("regionName").GetString());
        Assert.Equal(2, jhb.GetProperty("donorCount").GetInt32());

        var cpt = data.Single(r => r.GetProperty("region").GetString() == "CPT");
        Assert.Equal(0, cpt.GetProperty("donorCount").GetInt32());
    }

    [Fact]
    public async Task GetDonorsByRegion_NonAdminUser_Forbidden()
    {
        var client = await SetupForRegionsAsync("Procurement");

        var response = await client.GetAsync("/api/v1/reports/donors-by-region");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsByRegion_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/donors-by-region");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> SetupForTypesAsync(string roleName)
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
            Email = $"test-types-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var meat = new LookupDonationType { Id = 1, Name = "Meat", IsActive = true, SortOrder = 1 };
        var dairy = new LookupDonationType { Id = 2, Name = "Dairy", IsActive = true, SortOrder = 2 };
        context.LookupDonationTypes.AddRange(meat, dairy);

        var donorWithMeat = MakeDonor("Donor With Meat", loginUser.Id, null);
        var otherDonorWithMeat = MakeDonor("Other Donor With Meat", loginUser.Id, null);
        context.Donors.AddRange(donorWithMeat, otherDonorWithMeat);
        await context.SaveChangesAsync();

        context.DonorDonationTypes.Add(new DonorDonationType { DonorId = donorWithMeat.Id, DonationTypeId = meat.Id });
        context.DonorDonationTypes.Add(new DonorDonationType { DonorId = otherDonorWithMeat.Id, DonationTypeId = meat.Id });
        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    [Fact]
    public async Task GetDonorsByType_ReturnsCountsIncludingZeroForEmptyTypes()
    {
        var client = await SetupForTypesAsync("Admin");

        var response = await client.GetAsync("/api/v1/reports/donors-by-type");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data").EnumerateArray().ToList();

        Assert.Equal(2, data.Count);

        var meat = data.Single(t => t.GetProperty("donationType").GetString() == "Meat");
        Assert.Equal(2, meat.GetProperty("donorCount").GetInt32());

        var dairy = data.Single(t => t.GetProperty("donationType").GetString() == "Dairy");
        Assert.Equal(0, dairy.GetProperty("donorCount").GetInt32());
    }

    [Fact]
    public async Task GetDonorsByType_NonAdminUser_Forbidden()
    {
        var client = await SetupForTypesAsync("Procurement");

        var response = await client.GetAsync("/api/v1/reports/donors-by-type");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsByType_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/donors-by-type");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> SetupForStatusAsync(string roleName)
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
            Email = $"test-status-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var activeDonor = MakeDonor("Active Donor", loginUser.Id, null);
        var otherActiveDonor = MakeDonor("Other Active Donor", loginUser.Id, null);
        var pendingDonor = MakeDonor("Pending Donor", loginUser.Id, null);
        pendingDonor.Status = DonorStatus.PendingReview;
        context.Donors.AddRange(activeDonor, otherActiveDonor, pendingDonor);
        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    [Fact]
    public async Task GetDonorsByStatus_ReturnsCountsForAllFourStatusesInFixedOrder()
    {
        var client = await SetupForStatusAsync("Admin");

        var response = await client.GetAsync("/api/v1/reports/donors-by-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data").EnumerateArray().ToList();

        Assert.Equal(4, data.Count);
        Assert.Equal(
            new[] { "Active", "PendingReview", "Lapsed", "Rejected" },
            data.Select(d => d.GetProperty("status").GetString()));

        var active = data.Single(s => s.GetProperty("status").GetString() == "Active");
        Assert.Equal(2, active.GetProperty("donorCount").GetInt32());

        var pending = data.Single(s => s.GetProperty("status").GetString() == "PendingReview");
        Assert.Equal(1, pending.GetProperty("donorCount").GetInt32());

        var lapsed = data.Single(s => s.GetProperty("status").GetString() == "Lapsed");
        Assert.Equal(0, lapsed.GetProperty("donorCount").GetInt32());

        var rejected = data.Single(s => s.GetProperty("status").GetString() == "Rejected");
        Assert.Equal(0, rejected.GetProperty("donorCount").GetInt32());
    }

    [Fact]
    public async Task GetDonorsByStatus_NonAdminUser_Forbidden()
    {
        var client = await SetupForStatusAsync("Procurement");

        var response = await client.GetAsync("/api/v1/reports/donors-by-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorsByStatus_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/donors-by-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
