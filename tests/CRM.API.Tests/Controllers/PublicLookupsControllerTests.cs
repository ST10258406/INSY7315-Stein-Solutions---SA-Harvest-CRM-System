namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CRM.API.Controllers;
using CRM.API.Extensions;
using CRM.Domain.Entities.Lookups;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// PublicLookupsController deliberately calls the exact same GetLookupQuery/
/// GetCodedLookupQuery handlers as the protected LookupsController — these tests
/// exist to prove the public route is wired up (anonymous, rate-limited, correct
/// query per endpoint), not to re-test the query logic itself (already covered by
/// GetLookupQueryHandlerTests/GetCodedLookupQueryHandlerTests).
/// </summary>
public class PublicLookupsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PublicLookupsControllerTests(WebApplicationFactory<Program> factory)
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
                {
                    services.Remove(d);
                }

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());

                services.AddDbContext<CrmDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForPublicLookupsTesting");
                });
            });
        });
    }

    private static async Task SeedLookupDataAsync(CrmDbContext context)
    {
        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });
        context.LookupBbbeeStatuses.Add(new LookupBbbeeStatus { Id = 1, Name = "Level 1", IsActive = true });
        context.LookupDonationTypes.Add(new LookupDonationType { Id = 1, Name = "Meat", IsActive = true });
        context.LookupProvinces.Add(new LookupProvince { Id = 1, Code = "GP", Name = "Gauteng", IsActive = true });
        context.LookupOperationalRegions.Add(new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true });

        await context.SaveChangesAsync();
    }

    public static IEnumerable<object[]> AllSevenEndpoints =>
    [
        ["company-types"],
        ["entity-types"],
        ["operational-regions"],
        ["donation-types"],
        ["donation-frequencies"],
        ["bbbee-statuses"],
        ["provinces"]
    ];

    [Theory]
    [MemberData(nameof(AllSevenEndpoints))]
    public async Task Endpoint_NoAuthHeader_Returns200NotUnauthorized(string route)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedLookupDataAsync(context);

        var response = await client.GetAsync($"/api/v1/public/lookups/{route}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllSevenEndpoints))]
    public async Task Endpoint_ReturnsOneSeededRowUnderDataKey(string route)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedLookupDataAsync(context);

        var response = await client.GetAsync($"/api/v1/public/lookups/{route}");
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(1, root.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public void Controller_IsAnonymousAndCarriesThePublicLookupsRateLimitPolicy()
    {
        var rateLimitAttribute = typeof(PublicLookupsController)
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>().SingleOrDefault();
        Assert.NotNull(rateLimitAttribute);
        Assert.Equal(RateLimitingExtensions.PublicLookupsPolicy, rateLimitAttribute!.PolicyName);

        var controllerAllowsAnonymous = typeof(PublicLookupsController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        Assert.True(controllerAllowsAnonymous);
    }

    [Fact]
    public async Task Provinces_ReturnsIdenticalShapeToTheProtectedEndpointsQuery()
    {
        // PublicLookupsController.GetProvinces and LookupsController.GetProvinces
        // both send the exact same GetCodedLookupQuery(CodedLookupType.Provinces) —
        // this locks in that the public response has the same { data: [...] }
        // envelope and coded-lookup shape (id/code/name), not a divergent DTO.
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedLookupDataAsync(context);

        var response = await client.GetAsync("/api/v1/public/lookups/provinces");
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var item = root.GetProperty("data")[0];

        Assert.Equal(1, item.GetProperty("id").GetInt32());
        Assert.Equal("GP", item.GetProperty("code").GetString());
        Assert.Equal("Gauteng", item.GetProperty("name").GetString());
    }
}
