namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;

/// <summary>
/// Persistence for <see cref="RefreshToken"/>. Tokens are always looked up by their
/// SHA-256 hash (see SecureTokens.Hash) — the raw value never reaches the database.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>Stages a new refresh token for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Tracked token matched by hash, with its user and roles. Used by Refresh (rotation) and Logout.</summary>
    Task<RefreshToken?> GetByHashWithUserAndRolesAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Tracked token matched by hash, or null. Used by Refresh to check a rotated token's replacement.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Tracked, non-revoked tokens in one rotation family. Used to revoke a whole chain.</summary>
    Task<List<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Tracked list of the user's non-revoked tokens. Used for "sign out everywhere" revokes.</summary>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
