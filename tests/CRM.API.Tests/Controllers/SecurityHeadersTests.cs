namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>F-11: baseline security headers on every API response, success and error alike.</summary>
public class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityHeadersTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForSecurityHeaders"));
            });
        });
    }

    private static void AssertBaselineHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.False(response.Headers.Contains("X-Powered-By"));
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task HealthResponse_CarriesSecurityHeaders()
    {
        // Status is irrelevant here (the Postgres health check can't reach the test host's fake DB);
        // the headers must be present either way.
        var response = await _factory.CreateClient().GetAsync("/health");

        AssertBaselineHeaders(response);
    }

    [Fact]
    public async Task ErrorResponse_CarriesSecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/donors"); // unauthenticated

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertBaselineHeaders(response);
    }

    [Fact]
    public async Task ValidationErrorResponse_CarriesSecurityHeaders()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.True((int)response.StatusCode >= 400);
        AssertBaselineHeaders(response);
    }
}
