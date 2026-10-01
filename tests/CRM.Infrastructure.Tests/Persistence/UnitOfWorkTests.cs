using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence;

/// <summary>
/// Real-Postgres tests for UnitOfWork.ExecuteInTransactionAsync — the guarantee refresh-token
/// rotation relies on: an immediate atomic claim plus the replacement insert become visible
/// to other connections together, never as a half-done "replaced by a token that doesn't exist".
/// </summary>
public class UnitOfWorkTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_unitofwork"))
        .Options;

    private async Task<RefreshToken> SeedActiveTokenAsync()
    {
        await using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"uow-{Guid.NewGuid():N}@example.com",
            FirstName = "Unit",
            LastName = "OfWork",
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
        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }

    private RefreshToken ReplacementFor(RefreshToken token) => new()
    {
        Id = Guid.NewGuid(),
        UserId = token.UserId,
        TokenHash = Guid.NewGuid().ToString("N"),
        FamilyId = token.FamilyId,
        ExpiresAt = token.ExpiresAt,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private async Task<(bool OldRevoked, bool ReplacementExists)> ObserveFromAnotherConnectionAsync(RefreshToken token, RefreshToken replacement)
    {
        await using var observer = new CrmDbContext(_options);
        var old = await observer.RefreshTokens.AsNoTracking().SingleAsync(rt => rt.Id == token.Id);
        var exists = await observer.RefreshTokens.AsNoTracking().AnyAsync(rt => rt.Id == replacement.Id);
        return (old.IsRevoked, exists);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_OtherConnectionsNeverSeeTheClaimWithoutTheReplacement()
    {
        var token = await SeedActiveTokenAsync();
        var replacement = ReplacementFor(token);

        await using var context = new CrmDbContext(_options);
        var repository = new RefreshTokenRepository(context);
        var unitOfWork = new UnitOfWork(context);
        (bool OldRevoked, bool ReplacementExists) duringTransaction = default;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Assert.True(await repository.TryClaimRotationAsync(token.Id, replacement.TokenHash, DateTimeOffset.UtcNow));

            // The claim's UPDATE has executed but not committed: the world still sees the old,
            // consistent state (old token live, no dangling "replaced by").
            duringTransaction = await ObserveFromAnotherConnectionAsync(token, replacement);

            await repository.AddAsync(replacement);
            await unitOfWork.SaveChangesAsync();
            return true;
        });

        Assert.Equal((false, false), duringTransaction);
        Assert.Equal((true, true), await ObserveFromAnotherConnectionAsync(token, replacement));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBackTheClaimWhenTheWorkFails()
    {
        var token = await SeedActiveTokenAsync();
        var replacement = ReplacementFor(token);

        await using var context = new CrmDbContext(_options);
        var repository = new RefreshTokenRepository(context);
        var unitOfWork = new UnitOfWork(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<bool>(async () =>
        {
            await repository.TryClaimRotationAsync(token.Id, replacement.TokenHash, DateTimeOffset.UtcNow);
            throw new InvalidOperationException("insert failed");
        }));

        // The claim never became visible: the token is still live and still rotatable.
        Assert.Equal((false, false), await ObserveFromAnotherConnectionAsync(token, replacement));
    }
}
