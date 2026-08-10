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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

public class DonorsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DonorsControllerTests(WebApplicationFactory<Program> factory)
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
                {
                    services.Remove(d);
                }

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());

                services.AddDbContext<CrmDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForDonorsTesting");
                });
            });
        });
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        context.Roles.Add(role);

        var password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };

        user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginCommand = new LoginCommand(user.Email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    private static async Task SeedDonorsAsync(CrmDbContext context, int count)
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var entityType = new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true };
        context.LookupCompanyTypes.Add(companyType);
        context.LookupEntityTypes.Add(entityType);
        context.LookupDonationFrequencies.Add(frequency);

        var creator = new User
        {
            Id = Guid.NewGuid(),
            Email = "creator@example.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "n/a"
        };
        context.Users.Add(creator);

        for (var i = 1; i <= count; i++)
        {
            context.Donors.Add(new Donor
            {
                Id = Guid.NewGuid(),
                CompanyName = $"Donor Company {i:00}",
                CompanyTypeId = companyType.Id,
                RegisteredCompanyName = $"Donor Company {i:00} (Pty) Ltd",
                EntityTypeId = entityType.Id,
                DonationFrequencyId = frequency.Id,
                Status = DonorStatus.Active,
                SubmissionSource = SubmissionSource.ManualCapture,
                CreatedByUserId = creator.Id
            });
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDonors_ProcurementUser_ReturnsPaginatedEnvelope()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorsAsync(context, 25);

        var response = await client.GetAsync("/api/v1/donors?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;

        var data = json.GetProperty("data");
        Assert.Equal(10, data.GetArrayLength());

        var pagination = json.GetProperty("pagination");
        Assert.Equal(2, pagination.GetProperty("page").GetInt32());
        Assert.Equal(10, pagination.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, pagination.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, pagination.GetProperty("totalPages").GetInt32());

        var firstDonor = data[0];
        Assert.True(firstDonor.TryGetProperty("id", out _));
        Assert.True(firstDonor.TryGetProperty("companyName", out _));
        Assert.True(firstDonor.TryGetProperty("companyType", out _));
        Assert.True(firstDonor.TryGetProperty("status", out _));
        Assert.True(firstDonor.TryGetProperty("operationalRegions", out _));
        Assert.True(firstDonor.TryGetProperty("donationTypes", out _));
        Assert.False(firstDonor.TryGetProperty("collectionAddress", out _));
        Assert.False(firstDonor.TryGetProperty("incomeTaxNumber", out _));
    }

    [Fact]
    public async Task GetDonors_InvalidPageSize_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorsAsync(context, 1);

        var response = await client.GetAsync("/api/v1/donors?page=1&pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.GetAsync("/api/v1/donors");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/donors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
