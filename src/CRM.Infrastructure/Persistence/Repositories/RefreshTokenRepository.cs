namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CrmDbContext _context;

    public RefreshTokenRepository(CrmDbContext context) => _context = context;

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashWithUserAndRolesAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryClaimRotationAsync(Guid tokenId, string replacementHash, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        // InMemory fallback for CRM.API.Tests only — see TryClaimGraceReplayAsync. Not safe
        // under concurrency; never runs against Postgres.
        if (!_context.Database.IsNpgsql())
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == tokenId, cancellationToken);
            if (token is null || token.IsRevoked)
                return false;

            token.Revoke(at, replacementHash);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // Conditional UPDATE: Postgres row-locks the token, so a concurrent rotation of the
        // same token re-evaluates "NOT IsRevoked" after the first commits and matches nothing.
        var claimed = await _context.RefreshTokens
            .Where(rt => rt.Id == tokenId && !rt.IsRevoked)
            .ExecuteUpdateAsync(set => set
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, at)
                .SetProperty(rt => rt.ReplacedByTokenHash, replacementHash), cancellationToken);

        return claimed == 1;
    }

    public async Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql())
        {
            var active = await _context.RefreshTokens
                .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var token in active)
                token.Revoke(at);
            await _context.SaveChangesAsync(cancellationToken);
            return active.Count;
        }

        return await _context.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
            .ExecuteUpdateAsync(set => set
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, at), cancellationToken);
    }

    public Task<int> CountOtherActiveInFamilyAsync(Guid familyId, Guid excludingTokenId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _context.RefreshTokens.AsNoTracking()
            .CountAsync(rt => rt.FamilyId == familyId && rt.Id != excludingTokenId && !rt.IsRevoked && rt.ExpiresAt > now, cancellationToken);
    }

    public async Task<bool> TryClaimGraceReplayAsync(Guid tokenId, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        // The InMemory provider used by CRM.API.Tests can't run ExecuteUpdate — same
        // constraint as DonorRepository.ClaimBySubmissionTokenAsync. This fallback is a plain
        // read-then-write and is NOT safe under concurrency; it must never run against
        // Postgres.
        if (!_context.Database.IsNpgsql())
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == tokenId, cancellationToken);
            if (token is null || token.GraceReplayedAt is not null)
                return false;

            token.GraceReplayedAt = at;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // One conditional UPDATE: Postgres row-locks the token, so a concurrent second
        // caller re-evaluates "GraceReplayedAt IS NULL" after the first commits and matches
        // zero rows.
        var claimed = await _context.RefreshTokens
            .Where(rt => rt.Id == tokenId && rt.GraceReplayedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(rt => rt.GraceReplayedAt, at), cancellationToken);

        return claimed == 1;
    }

    public Task<List<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

    public Task<bool> IsSessionActiveAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _context.RefreshTokens.AsNoTracking()
            .AnyAsync(rt => rt.FamilyId == familyId && rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now, cancellationToken);
    }

    public Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
}
