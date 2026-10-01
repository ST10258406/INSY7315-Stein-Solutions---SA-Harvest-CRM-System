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

    /// <summary>
    /// Atomically rotates an active token: marks it revoked and replaced by
    /// <paramref name="replacementHash"/>, but only if it is still unrevoked. Returns true for
    /// the single caller that won; a concurrent rotation of the same token gets false.
    /// </summary>
    Task<bool> TryClaimRotationAsync(Guid tokenId, string replacementHash, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every still-active token in a family in one set-based statement (not
    /// load-then-loop), returning how many were revoked. Used by reuse detection.
    /// </summary>
    Task<int> RevokeFamilyAsync(Guid familyId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only count of the family's active tokens other than <paramref name="excludingTokenId"/>.
    /// Zero means the rest of the session was revoked (e.g. by reuse detection).
    /// </summary>
    Task<int> CountOtherActiveInFamilyAsync(Guid familyId, Guid excludingTokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks a rotated token's one grace replay as used. Returns true only for the
    /// single caller that claimed it; every other (including concurrent) caller gets false.
    /// </summary>
    Task<bool> TryClaimGraceReplayAsync(Guid tokenId, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Tracked, non-revoked tokens in one rotation family. Used to revoke a whole chain.</summary>
    Task<List<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the session (refresh-token family) still has a live token for this user.
    /// Checked on every authenticated request so revoking a session also kills its access tokens.
    /// </summary>
    Task<bool> IsSessionActiveAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tracked list of the user's non-revoked tokens. Used for "sign out everywhere" revokes.</summary>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
