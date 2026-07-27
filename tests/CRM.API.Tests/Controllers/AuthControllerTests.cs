using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Refresh;
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
}
