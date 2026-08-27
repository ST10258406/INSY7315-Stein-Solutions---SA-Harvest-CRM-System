namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Entities;

public interface IRefreshTokenRepository
{
    /// <summary>Stages a new refresh token for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Tracked token matched by its value. Used by Logout to revoke.</summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Read-only token with its user and that user's roles eagerly loaded. Used by Refresh.</summary>
    Task<RefreshToken?> GetByTokenWithUserAndRolesAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Tracked list of the user's non-revoked tokens. Used by ResetPassword's bulk revoke.</summary>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
