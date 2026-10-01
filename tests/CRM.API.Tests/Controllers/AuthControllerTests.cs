using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hangfire;
using NSubstitute;

namespace CRM.API.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
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
                // Remove existing EF Core and DbContext registrations
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

                // Override Hangfire to use InMemoryStorage
                services.AddHangfire(config => config.UseInMemoryStorage());
                
                services.AddDbContext<CrmDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Swap the real Brevo-backed EmailService for a no-op fake so
                // ForgotPassword integration tests never make a real outbound
                // HTTP call.
                services.RemoveAll<IEmailService>();
                services.AddScoped<IEmailService>(_ => Substitute.For<IEmailService>());
            });
        });
    }

    [Fact]
    public async Task Login_WithValidCreds_Returns200AndExpectedShape()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        // Ensure database is created using Sqlite provider instead of Migrations
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test-integration@example.com",
            FirstName = "Integration",
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var command = new LoginCommand(user.Email, password);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, but got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<LoginResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(user.Id, result.User.Id);

        // The refresh token is only ever in the HttpOnly cookie — never in the JSON body,
        // where JavaScript (and so XSS) could read it.
        Assert.DoesNotContain("refreshToken", content, StringComparison.OrdinalIgnoreCase);
        var setCookie = AuthCookieTestHelpers.GetRefreshSetCookieHeader(response);
        Assert.NotNull(setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        // Only the hash is stored.
        var raw = AuthCookieTestHelpers.GetRefreshCookie(response)!;
        using var verifyScope = _factory.Services.CreateScope();
        var stored = await verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>()
            .RefreshTokens.AsNoTracking().SingleAsync(rt => rt.UserId == user.Id);
        Assert.NotEqual(raw, stored.TokenHash);
        Assert.Equal(SecureTokens.Hash(raw), stored.TokenHash);
    }

    [Fact]
    public async Task Login_WithBadCreds_Returns401WithStandardEnvelope()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var command = new LoginCommand("nonexistent@example.com", "WrongPassword");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseString);

        Assert.Equal(401, result.GetProperty("status").GetInt32());
        Assert.Equal("UNAUTHORIZED", result.GetProperty("code").GetString());
        Assert.Equal("Invalid email or password.", result.GetProperty("message").GetString());
    }

    private async Task<User> ResetDbWithUserAsync(string email, string password = "TestPassword123", bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Cookie",
            LastName = "Test",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            IsActive = isActive,
            UserRoles = new List<UserRole>()
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<User> AddUserAsync(string email, string password = "TestPassword123")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Second",
            LastName = "User",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private async Task<List<RefreshToken>> TokensForAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CrmDbContext>()
            .RefreshTokens.AsNoTracking().Where(rt => rt.UserId == userId).ToListAsync();
    }

    [Fact]
    public async Task Refresh_WithValidCookie_Returns200WithUserAndRotatesCookie()
    {
        var user = await ResetDbWithUserAsync("refresh-api@example.com");
        var (_, original) = await _factory.LoginForCookieAsync(user.Email, "TestPassword123");

        var response = await _factory.RefreshWithCookieAsync(original);

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, but got {response.StatusCode}. Content: {content}");
        var result = JsonSerializer.Deserialize<RefreshTokenResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal(user.Id, result.User.Id); // enough for the SPA to restore its session
        Assert.DoesNotContain("refreshToken", content, StringComparison.OrdinalIgnoreCase);

        // Rotation: a new cookie value, the old token retired in favour of it, same family.
        var rotated = AuthCookieTestHelpers.GetRefreshCookie(response);
        Assert.NotNull(rotated);
        Assert.NotEqual(original, rotated);
        var tokens = await TokensForAsync(user.Id);
        var old = tokens.Single(t => t.TokenHash == SecureTokens.Hash(original));
        var replacement = tokens.Single(t => t.TokenHash == SecureTokens.Hash(rotated));
        Assert.True(old.IsRevoked);
        Assert.Equal(replacement.TokenHash, old.ReplacedByTokenHash);
        Assert.Equal(old.FamilyId, replacement.FamilyId);
        Assert.False(replacement.IsRevoked);
    }

    [Fact]
    public async Task Refresh_PageReload_RestoresSessionFromCookieAlone()
    {
        // What the SPA does on page load: no access token in memory, only the cookie the
        // browser kept from login.
        var user = await ResetDbWithUserAsync("reload@example.com");
        var browser = _factory.CreateBrowserClient();
        (await browser.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, "TestPassword123"))).EnsureSuccessStatusCode();

        var first = await browser.PostAsync("/api/auth/refresh", null);
        var second = await browser.PostAsync("/api/auth/refresh", null); // next reload, rotated cookie

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var restored = JsonSerializer.Deserialize<RefreshTokenResponseDto>(
            await second.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.Equal(user.Email, restored.User.Email);
    }

    [Fact]
    public async Task Refresh_WithBadCookie_Returns401WithStandardEnvelopeAndClearsCookie()
    {
        await ResetDbWithUserAsync("bad-cookie@example.com");

        var response = await _factory.RefreshWithCookieAsync("invalid-or-expired-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var result = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, result.GetProperty("status").GetInt32());
        Assert.Equal("UNAUTHORIZED", result.GetProperty("code").GetString());
        Assert.Equal("Refresh token is invalid or expired.", result.GetProperty("message").GetString());
        Assert.True(result.TryGetProperty("traceId", out _));

        var setCookie = AuthCookieTestHelpers.GetRefreshSetCookieHeader(response);
        Assert.NotNull(setCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var response = await _factory.RefreshWithCookieAsync(refreshToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutCsrfHeader_Returns403()
    {
        var user = await ResetDbWithUserAsync("csrf@example.com");
        var (_, cookie) = await _factory.LoginForCookieAsync(user.Email, "TestPassword123");

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{AuthCookieTestHelpers.CookieName}={Uri.EscapeDataString(cookie)}");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        // Nothing rotated: the session is untouched.
        Assert.All(await TokensForAsync(user.Id), t => Assert.False(t.IsRevoked));
    }

    [Fact]
    public async Task Refresh_WithDeactivatedUser_Returns401()
    {
        var user = await ResetDbWithUserAsync("deactivated-refresh@example.com");
        var (_, cookie) = await _factory.LoginForCookieAsync(user.Email, "TestPassword123");
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            (await context.Users.SingleAsync(u => u.Id == user.Id)).IsActive = false;
            await context.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.RefreshWithCookieAsync(cookie)).StatusCode);
    }

    [Fact]
    public async Task Refresh_ReplayedOldCookieWithinGraceWindow_StillWorks()
    {
        // Two tabs reloading at once both send the same cookie; neither may be logged out.
        var user = await ResetDbWithUserAsync("two-tabs@example.com");
        var (_, original) = await _factory.LoginForCookieAsync(user.Email, "TestPassword123");

        var tabA = await _factory.RefreshWithCookieAsync(original);
        var tabB = await _factory.RefreshWithCookieAsync(original);

        Assert.Equal(HttpStatusCode.OK, tabA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tabB.StatusCode);
        // Both tabs' new cookies keep working afterwards.
        Assert.Equal(HttpStatusCode.OK, (await _factory.RefreshWithCookieAsync(AuthCookieTestHelpers.GetRefreshCookie(tabA))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _factory.RefreshWithCookieAsync(AuthCookieTestHelpers.GetRefreshCookie(tabB))).StatusCode);
    }

    [Fact]
    public async Task Refresh_ReplayedOldCookieAfterGraceWindow_RevokesWholeFamily()
    {
        // F-04 reuse detection. Grace window set to 0 on this host so "later" is immediate.
        using var factory = _factory.WithWebHostBuilder(b => b.UseSetting("Auth:RefreshToken:ReuseGraceSeconds", "0"));
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "stolen@example.com",
                FirstName = "Stolen",
                LastName = "Cookie",
                PasswordHash = new PasswordHasher<User>().HashPassword(null!, "TestPassword123")
            });
            await context.SaveChangesAsync();
        }

        var (_, original) = await factory.LoginForCookieAsync("stolen@example.com", "TestPassword123");
        var legit = await factory.RefreshWithCookieAsync(original);
        Assert.Equal(HttpStatusCode.OK, legit.StatusCode);
        var current = AuthCookieTestHelpers.GetRefreshCookie(legit)!;
        await Task.Delay(1100); // past the (zero-second) grace window

        // The attacker replays the copied original...
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.RefreshWithCookieAsync(original)).StatusCode);

        // ...which kills the whole chain, including the legitimate user's current cookie.
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.RefreshWithCookieAsync(current)).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ExistingUser_Returns200WithGenericMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "forgot-pass@example.com",
            FirstName = "Forgot",
            LastName = "Password",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "forgot-pass@example.com" });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentUser_ReturnsIdentical200WithGenericMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "doesnotexist@example.com" });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);
    }

    [Fact]
    public async Task Logout_WithValidAccessToken_Returns204RevokesSessionAndClearsCookie()
    {
        var user = await ResetDbWithUserAsync("logout-integration@example.com");
        var browser = _factory.CreateBrowserClient();
        var loginResponse = await browser.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, "TestPassword123"));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await loginResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var cookie = AuthCookieTestHelpers.GetRefreshCookie(loginResponse)!;
        browser.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        var response = await browser.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", AuthCookieTestHelpers.GetRefreshSetCookieHeader(response)!, StringComparison.OrdinalIgnoreCase);
        Assert.All(await TokensForAsync(user.Id), t => Assert.True(t.IsRevoked));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.RefreshWithCookieAsync(cookie)).StatusCode);

        // The access token belonged to that session, so it stops working immediately.
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_PresentingAnotherUsersCookie_LeavesTheirSessionAlone()
    {
        // F-21: logout must only ever revoke the caller's own session.
        var victim = await ResetDbWithUserAsync("victim-session@example.com");
        var attacker = await AddUserAsync("attacker-session@example.com");
        var (_, victimCookie) = await _factory.LoginForCookieAsync(victim.Email, "TestPassword123");
        var (attackerLogin, _) = await _factory.LoginForCookieAsync(attacker.Email, "TestPassword123");

        var client = _factory.CreateBrowserClient(handleCookies: false);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", attackerLogin.AccessToken);
        request.Headers.Add("Cookie", $"{AuthCookieTestHelpers.CookieName}={Uri.EscapeDataString(victimCookie)}");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.All(await TokensForAsync(victim.Id), t => Assert.False(t.IsRevoked));
        Assert.Equal(HttpStatusCode.OK, (await _factory.RefreshWithCookieAsync(victimCookie)).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_Returns401()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidCommand_Returns200AndUpdatesPassword()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "reset-integration@example.com",
            FirstName = "Reset",
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, "OldPassword123!"),
            PasswordResetTokenHash = SecureTokens.Hash("integration-reset-token"),
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            UserRoles = new List<UserRole>()
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SecureTokens.Hash("active-refresh"),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var command = new ResetPasswordCommand(
            "integration-reset-token",
            "reset-integration@example.com",
            "NewPassword123!",
            "NewPassword123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<ResetPasswordResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Password has been reset successfully.", result!.Message);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var userInDb = await assertContext.Users.FindAsync(user.Id);
        Assert.Null(userInDb!.PasswordResetTokenHash);
        Assert.Null(userInDb.PasswordResetTokenExpiresAt);

        var verifyResult = hasher.VerifyHashedPassword(userInDb, userInDb.PasswordHash, "NewPassword123!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);

        var tokenInDb = await assertContext.RefreshTokens.FindAsync(refreshToken.Id);
        Assert.True(tokenInDb!.IsRevoked);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new ResetPasswordCommand(
            "wrong-token",
            "doesnotexist@example.com",
            "NewPassword123!",
            "NewPassword123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidAccessToken_Returns204AndUpdatesPassword()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "change-password@example.com",
            FirstName = "Change",
            LastName = "Password",
            PasswordHash = hasher.HashPassword(null!, "CurrentPassword123!"),
            UserRoles = new List<UserRole>()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginCommand = new LoginCommand(user.Email, "CurrentPassword123!");
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        var changeCommand = new ChangePasswordCommand("CurrentPassword123!", "NewPassword123!", "NewPassword123!");

        // Act
        var response = await client.PatchAsJsonAsync("/api/auth/change-password", changeCommand);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var userInDb = await assertContext.Users.FindAsync(user.Id);

        var verifyResult = hasher.VerifyHashedPassword(userInDb!, userInDb!.PasswordHash, "NewPassword123!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task ChangePassword_WithoutAccessToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!");

        // Act
        var response = await client.PatchAsJsonAsync("/api/auth/change-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_EndsOtherSessionsButKeepsTheCallersOwn()
    {
        // F-05: a stolen session must not survive a password change.
        var user = await ResetDbWithUserAsync("two-devices@example.com", "CurrentPassword123!");
        var (otherDeviceLogin, otherDeviceCookie) = await _factory.LoginForCookieAsync(user.Email, "CurrentPassword123!");

        var thisDevice = _factory.CreateBrowserClient();
        var login = await thisDevice.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, "CurrentPassword123!"));
        var accessToken = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.AccessToken;
        thisDevice.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var change = await thisDevice.PatchAsJsonAsync("/api/auth/change-password",
            new ChangePasswordCommand("CurrentPassword123!", "NewPassword123!", "NewPassword123!"));

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.RefreshWithCookieAsync(otherDeviceCookie)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await thisDevice.PostAsync("/api/auth/refresh", null)).StatusCode);

        // The other device's live access token is cut off at once.
        var otherDevice = _factory.CreateBrowserClient(handleCookies: false);
        otherDevice.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", otherDeviceLogin.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.PatchAsJsonAsync("/api/auth/change-password",
            new ChangePasswordCommand("NewPassword123!", "Another123!", "Another123!"))).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_SameLinkUsedTwice_SecondAttemptFails()
    {
        var user = await ResetDbWithUserAsync("single-use@example.com");
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var tracked = await context.Users.SingleAsync(u => u.Id == user.Id);
            tracked.PasswordResetTokenHash = SecureTokens.Hash("one-time-token");
            tracked.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
            await context.SaveChangesAsync();
        }
        var client = _factory.CreateClient();
        var command = new ResetPasswordCommand("one-time-token", user.Email, "NewPassword123!", "NewPassword123!");

        var first = await client.PostAsJsonAsync("/api/auth/reset-password", command);
        var second = await client.PostAsJsonAsync("/api/auth/reset-password", command with { NewPassword = "Another123!", ConfirmPassword = "Another123!" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_OldCookieReplayedAThirdTime_RevokesWholeFamily()
    {
        // Two tabs racing is fine (one grace replay); a further replay of the same old cookie
        // inside the window is not, and must not keep minting new sessions.
        var user = await ResetDbWithUserAsync("third-replay@example.com");
        var (_, original) = await _factory.LoginForCookieAsync(user.Email, "TestPassword123");

        var tabA = await _factory.RefreshWithCookieAsync(original);
        var tabB = await _factory.RefreshWithCookieAsync(original);
        var third = await _factory.RefreshWithCookieAsync(original);

        Assert.Equal(HttpStatusCode.OK, tabA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tabB.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, third.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.RefreshWithCookieAsync(AuthCookieTestHelpers.GetRefreshCookie(tabA))).StatusCode);
    }
}
