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

public class CreateDonorControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateDonorControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("InMemoryDbForCreateDonorTesting");
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

    private static async Task SeedLookupsAsync(CrmDbContext context)
    {
        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });
        context.LookupProvinces.Add(new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true });
        context.LookupOperationalRegions.Add(new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true });
        context.LookupDonationTypes.Add(new LookupDonationType { Id = 1, Name = "Meat", IsActive = true });
        await context.SaveChangesAsync();
    }

    private static object MakeValidPayload(string incomeTaxNumber = "9012345678") => new
    {
        company = new
        {
            companyName = "Test Co",
            companyTypeId = 1,
            website = "https://test.co.za",
            registeredCompanyName = "Test Co (Pty) Ltd",
            tradingName = "Test Co",
            entityTypeId = 1,
            companyRegistrationNumber = "2020/000000/07",
            incomeTaxNumber
        },
        primaryContact = new { name = "Jane Tester", phone = "+27820000000", email = "jane@test.co.za" },
        legalAddress = new
        {
            streetAddress = "1 Test Street",
            suburb = "Testville",
            city = "Johannesburg",
            provinceId = 3,
            postalCode = "2000"
        },
        donations = new
        {
            frequencyId = 1,
            typeIds = new[] { 1 },
            collectionAddress = "Gate 1",
            regionIds = new[] { 1 }
        }
    };

    [Fact]
    public async Task CreateDonor_ValidPayload_Returns201WithPendingReviewAndManualCapture()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedLookupsAsync(context);

        var response = await client.PostAsJsonAsync("/api/v1/donors", MakeValidPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(content).RootElement.GetProperty("data");

        Assert.Equal("PendingReview", data.GetProperty("status").GetString());
        Assert.Equal("ManualCapture", data.GetProperty("submissionSource").GetString());

        var donorId = data.GetProperty("id").GetGuid();

        var getResponse = await client.GetAsync($"/api/v1/donors/{donorId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getData = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        Assert.Equal("Test Co", getData.GetProperty("company").GetProperty("companyName").GetString());
        Assert.Equal("Jane Tester", getData.GetProperty("primaryContact").GetProperty("name").GetString());

        var approval = await context.DonorApprovals.SingleAsync(a => a.DonorId == donorId);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);

        var regionRow = await context.DonorOperationalRegions.SingleAsync(r => r.DonorId == donorId);
        Assert.Equal((short)1, regionRow.OperationalRegionId);

        var typeRow = await context.DonorDonationTypes.SingleAsync(t => t.DonorId == donorId);
        Assert.Equal((short)1, typeRow.DonationTypeId);
    }

    [Fact]
    public async Task CreateDonor_IncomeTaxNumberStartingWith4_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedLookupsAsync(context);

        var response = await client.PostAsJsonAsync("/api/v1/donors", MakeValidPayload(incomeTaxNumber: "4012345678"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonor_MissingRegionIds_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedLookupsAsync(context);

        var payload = new
        {
            company = new
            {
                companyName = "Test Co",
                companyTypeId = 1,
                website = "https://test.co.za",
                registeredCompanyName = "Test Co (Pty) Ltd",
                tradingName = "Test Co",
                entityTypeId = 1,
                companyRegistrationNumber = "2020/000000/07",
                incomeTaxNumber = "9012345678"
            },
            primaryContact = new { name = "Jane Tester", phone = "+27820000000", email = "jane@test.co.za" },
            legalAddress = new
            {
                streetAddress = "1 Test Street",
                suburb = "Testville",
                city = "Johannesburg",
                provinceId = 3,
                postalCode = "2000"
            },
            donations = new
            {
                frequencyId = 1,
                typeIds = new[] { 1 },
                collectionAddress = "Gate 1",
                regionIds = Array.Empty<int>()
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/donors", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonor_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.PostAsJsonAsync("/api/v1/donors", MakeValidPayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonor_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/donors", MakeValidPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
