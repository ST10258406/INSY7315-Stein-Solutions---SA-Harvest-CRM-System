namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Text.Json;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

public class RateLimitTestControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitTestControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("InMemoryDbForRateLimitTesting");
                });
            });
        });
    }

    [Fact]
    public async Task PublicSubmitPolicy_Allows10PerHour_Then429sWithStandardEnvelope()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsync("/api/v1/rate-limit-test/submit", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rejected = await client.PostAsync("/api/v1/rate-limit-test/submit", null);
        Assert.Equal((HttpStatusCode)429, rejected.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await rejected.Content.ReadAsStringAsync());
        Assert.Equal(429, body.GetProperty("status").GetInt32());
        Assert.Equal("RATE_LIMITED", body.GetProperty("code").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task PublicLookupsPolicy_Allows60PerMinute_Then429s()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 60; i++)
        {
            var response = await client.GetAsync("/api/v1/rate-limit-test/lookups");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rejected = await client.GetAsync("/api/v1/rate-limit-test/lookups");
        Assert.Equal((HttpStatusCode)429, rejected.StatusCode);
    }
}
