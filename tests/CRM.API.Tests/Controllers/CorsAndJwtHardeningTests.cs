namespace CRM.API.Tests.Controllers;

using System.Net;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// F-03 / F-20: CORS origins come from configuration (appsettings.Testing.json allows only
/// https://frontend.test) and JWTs are pinned to HS256.
/// </summary>
public class CorsAndJwtHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AllowedOrigin = "https://frontend.test";
    private readonly WebApplicationFactory<Program> _factory;

    public CorsAndJwtHardeningTests(WebApplicationFactory<Program> factory)
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForCorsJwtTesting"));
            });
        });
    }

    private async Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return await _factory.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Preflight_FromConfiguredOrigin_IsAllowedWithCredentials()
    {
        var response = await PreflightAsync(AllowedOrigin);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowed));
        Assert.Equal(AllowedOrigin, Assert.Single(allowed));
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials));
        Assert.Equal("true", Assert.Single(credentials));
    }

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("https://evil.example")]
    public async Task Preflight_FromUnlistedOrigin_GetsNoCorsHeaders(string origin)
    {
        var response = await PreflightAsync(origin);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void JwtBearer_ValidAlgorithms_IsPinnedToHs256()
    {
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal([SecurityAlgorithms.HmacSha256], options.TokenValidationParameters.ValidAlgorithms);
    }
}
