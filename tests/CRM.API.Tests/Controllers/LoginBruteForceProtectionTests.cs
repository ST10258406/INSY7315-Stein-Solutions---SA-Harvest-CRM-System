namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// F-02: strict per-IP rate limits on the anonymous auth endpoints, plus a per-account
/// lockout after consecutive failed logins that an Admin can clear.
/// </summary>
public class LoginBruteForceProtectionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "CorrectPassword123!";
    private readonly WebApplicationFactory<Program> _baseFactory;

    public LoginBruteForceProtectionTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        _baseFactory = factory;
    }

    /// <summary>
    /// A fresh host (own limiter state, own in-memory DB). Settings are applied via
    /// UseSetting rather than environment variables so they can't leak into test classes
    /// running in parallel.
    /// </summary>
    private WebApplicationFactory<Program> CreateFactory(params (string Key, string Value)[] settings)
    {
        var databaseName = $"InMemoryDbForBruteForce-{Guid.NewGuid():N}";
        return _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);

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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }

    private static async Task<User> SeedUserAsync(WebApplicationFactory<Program> factory, string email, string roleName = "Marketing")
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
            ?? context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = roleName }).Entity;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Brute",
            LastName = "Force",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, Password),
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id });
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static Task<HttpResponseMessage> LoginAsync(WebApplicationFactory<Program> factory, string email, string password) =>
        factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));

    private static async Task<string> ErrorBodyWithoutTraceIdAsync(HttpResponseMessage response)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return string.Join("|", json.EnumerateObject().Where(p => p.Name != "traceId").Select(p => $"{p.Name}={p.Value}"));
    }

    [Fact]
    public async Task Login_OverPerIpLimit_Returns429WithRetryAfterInStandardEnvelope()
    {
        using var factory = CreateFactory(("RateLimiting:Auth:Login:PermitLimit", "3"));

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(factory, $"nobody{i}@example.com", "wrong")).StatusCode);

        var limited = await LoginAsync(factory, "nobody@example.com", "wrong");

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var json = JsonDocument.Parse(await limited.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", json.GetProperty("code").GetString());
        Assert.True(json.TryGetProperty("traceId", out _));
    }

    [Theory]
    [InlineData("RateLimiting:Auth:ResetPassword:PermitLimit", "/api/auth/reset-password")]
    [InlineData("RateLimiting:Auth:Refresh:PermitLimit", "/api/auth/refresh")]
    public async Task OtherAuthEndpoints_OverPerIpLimit_Return429(string setting, string path)
    {
        using var factory = CreateFactory((setting, "2"));
        var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(path, new { })).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync(path, new { })).StatusCode);
    }

    [Fact]
    public async Task Login_FiveFailures_LocksAccountEvenForCorrectPassword_WithIndistinguishableResponse()
    {
        using var factory = CreateFactory();
        var user = await SeedUserAsync(factory, "victim@example.com");

        HttpResponseMessage wrongPassword = null!;
        for (var i = 0; i < 5; i++)
            wrongPassword = await LoginAsync(factory, user.Email, "WrongPassword!");

        var lockedWithCorrectPassword = await LoginAsync(factory, user.Email, Password);
        var unknownEmail = await LoginAsync(factory, "nobody-here@example.com", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, lockedWithCorrectPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);

        // Same status and body (traceId aside) for locked, wrong password and unknown email.
        var lockedBody = await ErrorBodyWithoutTraceIdAsync(lockedWithCorrectPassword);
        Assert.Equal(lockedBody, await ErrorBodyWithoutTraceIdAsync(wrongPassword));
        Assert.Equal(lockedBody, await ErrorBodyWithoutTraceIdAsync(unknownEmail));
    }

    [Fact]
    public async Task Login_AfterLockExpires_CorrectPasswordSucceedsAndResetsCounter()
    {
        using var factory = CreateFactory();
        var user = await SeedUserAsync(factory, "expired-lock@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var tracked = await context.Users.SingleAsync(u => u.Id == user.Id);
            tracked.FailedLoginCount = 3;
            tracked.LockoutEndUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
            await context.SaveChangesAsync();
        }

        var response = await LoginAsync(factory, user.Email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyScope = factory.Services.CreateScope();
        var reloaded = await verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>()
            .Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.Equal(0, reloaded.FailedLoginCount);
        Assert.Null(reloaded.LockoutEndUtc);
    }

    [Fact]
    public async Task Unlock_ByAdmin_ClearsLockSoUserCanLogInAgain()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "admin@example.com", "Admin");
        var victim = await SeedUserAsync(factory, "locked-out@example.com");

        for (var i = 0; i < 5; i++)
            await LoginAsync(factory, victim.Email, "WrongPassword!");
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(factory, victim.Email, Password)).StatusCode);

        var adminClient = await AuthenticatedClientAsync(factory, "admin@example.com");

        // The Users screen shows the lock (drives the badge and the Unlock menu item).
        var listed = await FindListedUserAsync(adminClient, victim.Id);
        Assert.True(listed.GetProperty("isLockedOut").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, listed.GetProperty("lockedUntil").ValueKind);

        var unlock = await adminClient.PostAsync($"/api/v1/users/{victim.Id}/unlock", null);

        Assert.Equal(HttpStatusCode.OK, unlock.StatusCode);
        var afterUnlock = await FindListedUserAsync(adminClient, victim.Id);
        Assert.False(afterUnlock.GetProperty("isLockedOut").GetBoolean());
        Assert.Equal(JsonValueKind.Null, afterUnlock.GetProperty("lockedUntil").ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(factory, victim.Email, Password)).StatusCode);
    }

    private static async Task<JsonElement> FindListedUserAsync(HttpClient client, Guid userId)
    {
        var json = JsonDocument.Parse(await client.GetStringAsync("/api/v1/users?pageSize=100")).RootElement;
        return json.GetProperty("data").EnumerateArray().Single(u => u.GetProperty("id").GetGuid() == userId);
    }

    [Fact]
    public async Task Unlock_AdminTargetingSuperAdmin_IsForbidden()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "admin@example.com", "Admin");
        var superAdmin = await SeedUserAsync(factory, "owner@example.com", "SuperAdmin");

        var adminClient = await AuthenticatedClientAsync(factory, "admin@example.com");
        var response = await adminClient.PostAsync($"/api/v1/users/{superAdmin.Id}/unlock", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unlock_NonAdmin_IsForbidden()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "marketing@example.com");
        var victim = await SeedUserAsync(factory, "colleague@example.com");

        var client = await AuthenticatedClientAsync(factory, "marketing@example.com");
        var response = await client.PostAsync($"/api/v1/users/{victim.Id}/unlock", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program> factory, string email)
    {
        var login = await LoginAsync(factory, email, Password);
        var dto = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto.AccessToken);
        return client;
    }
}
