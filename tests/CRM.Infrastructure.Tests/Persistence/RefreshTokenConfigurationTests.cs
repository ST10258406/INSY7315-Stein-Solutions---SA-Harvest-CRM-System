using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence;

public class RefreshTokenConfigurationTests
{
    private const string TestConnectionString = "Host=localhost;Database=crm_test_refreshtoken;Username=postgres;Password=P@ss1234ID";
    private readonly DbContextOptions<CrmDbContext> _options;

    public RefreshTokenConfigurationTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
    }

    [Fact]
    public async Task Insert_DuplicateToken_ThrowsException()
    {
        // Arrange
        using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid()}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var tokenString = $"duplicate-token-{Guid.NewGuid()}";
        
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = tokenString,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.RefreshTokens.Add(token1);
        await context.SaveChangesAsync();

        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id, // same user or different user doesn't matter
            Token = tokenString, // duplicate!
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.RefreshTokens.Add(token2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteUser_CascadesToRefreshTokens()
    {
        // Arrange
        using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test2-{Guid.NewGuid()}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "unique-token-for-user",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        context.Users.Remove(user);
        await context.SaveChangesAsync();

        // Assert
        var tokenExists = await context.RefreshTokens.AnyAsync(rt => rt.Id == token.Id);
        Assert.False(tokenExists);
    }
}
