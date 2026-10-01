using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Real-Postgres tests for the parts of RefreshTokenRepository whose correctness depends on
/// the database itself (atomic conditional UPDATE), not on EF's InMemory fallback.
/// </summary>
public class RefreshTokenRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_refreshtokenrepo"))
        .Options;

    private async Task<RefreshToken> SeedRotatedTokenAsync()
    {
        await using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"grace-{Guid.NewGuid():N}@example.com",
            FirstName = "Grace",
            LastName = "Replay",
            PasswordHash = "hash"
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Guid.NewGuid().ToString("N"),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };
        token.Revoke(DateTimeOffset.UtcNow, replacedByTokenHash: Guid.NewGuid().ToString("N"));

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task TryClaimGraceReplayAsync_FirstCallWins_SecondLoses()
    {
        var token = await SeedRotatedTokenAsync();

        await using var context = new CrmDbContext(_options);
        var repository = new RefreshTokenRepository(context);

        Assert.True(await repository.TryClaimGraceReplayAsync(token.Id, DateTimeOffset.UtcNow));
        Assert.False(await repository.TryClaimGraceReplayAsync(token.Id, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task TryClaimGraceReplayAsync_ConcurrentCallers_ExactlyOneWins()
    {
        // The whole point of the conditional UPDATE: two tabs (or an attacker) racing for the
        // single grace replay can't both get it.
        var token = await SeedRotatedTokenAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var context = new CrmDbContext(_options);
            return await new RefreshTokenRepository(context).TryClaimGraceReplayAsync(token.Id, DateTimeOffset.UtcNow);
        }));

        Assert.Equal(1, results.Count(claimed => claimed));
    }

    [Fact]
    public async Task IsSessionActiveAsync_FalseOnceTheFamilyIsRevoked()
    {
        var token = await SeedRotatedTokenAsync(); // revoked, and its family has nothing live

        await using var context = new CrmDbContext(_options);
        var repository = new RefreshTokenRepository(context);

        Assert.False(await repository.IsSessionActiveAsync(token.FamilyId, token.UserId));
    }
}
