using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Logout;
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
using Hangfire;

namespace CRM.API.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "TestAudience");
        
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
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal(user.Id, result.User.Id);
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

    [Fact]
    public async Task Refresh_WithValidToken_Returns200AndExpectedShape()
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
            Email = "refresh-api@example.com",
            FirstName = "API",
            LastName = "Test",
            PasswordHash = "hash",
            UserRoles = new List<UserRole>()
        };
        context.Users.Add(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "integration-valid-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        var command = new RefreshTokenCommand("integration-valid-refresh-token");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, but got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<RefreshTokenResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task Refresh_WithBadToken_Returns401WithStandardEnvelope()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var command = new RefreshTokenCommand("invalid-or-expired-token");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseString);

        Assert.Equal(401, result.GetProperty("status").GetInt32());
        Assert.Equal("UNAUTHORIZED", result.GetProperty("code").GetString());
        Assert.Equal("Refresh token is invalid or expired.", result.GetProperty("message").GetString());
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

        var command = new ForgotPasswordCommand("forgot-pass@example.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", command);

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

        var command = new ForgotPasswordCommand("doesnotexist@example.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", command);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK, got {response.StatusCode}. Content: {content}");

        var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);
    }

    [Fact]
    public async Task Logout_WithValidAccessToken_Returns204()
    {
        // Arrange
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "logout-integration@example.com",
            FirstName = "Logout",
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginCommand = new LoginCommand(user.Email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        var logoutCommand = new LogoutCommand(loginResult.RefreshToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", logoutCommand);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the token was revoked in the DB
        var tokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == loginResult.RefreshToken);
        Assert.NotNull(tokenInDb);
        Assert.True(tokenInDb.IsRevoked);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = new LogoutCommand("some-refresh-token");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/logout", command);

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
            PasswordResetToken = "integration-reset-token",
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            UserRoles = new List<UserRole>()
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "active-refresh",
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
        Assert.Null(userInDb!.PasswordResetToken);
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
}
