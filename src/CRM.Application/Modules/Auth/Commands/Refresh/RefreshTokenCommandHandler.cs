using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Application.Modules.Auth.Commands.Refresh;

/// <summary>
/// Rotating refresh (security review F-04): every successful call revokes the presented
/// token and issues a new one in the same family. A rotated token presented again outside
/// the short grace window means it was copied, so the whole family is revoked and the user
/// has to log in again.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDto>
{
    private const string GenericFailureMessage = "Refresh token is invalid or expired.";

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly RefreshTokenOptions _options;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<RefreshTokenOptions> options,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RefreshTokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        // No cookie is just "not signed in" — the same 401 as an invalid token, not a 400.
        if (string.IsNullOrEmpty(request.RefreshToken))
            throw new UnauthorizedException(GenericFailureMessage);

        var now = DateTimeOffset.UtcNow;
        var storedToken = await _refreshTokens.GetByHashWithUserAndRolesAsync(SecureTokens.Hash(request.RefreshToken), ct);

        // Same generic 401 for every failure — "doesn't exist", "revoked", "expired", "user
        // deactivated", "reuse detected" — don't tell the caller which case it was.
        if (storedToken is null)
            throw new UnauthorizedException(GenericFailureMessage);

        // Captured up front: the atomic claims below update the row in the database, and on
        // the test provider also this tracked instance.
        var isGraceReplay = storedToken.IsRevoked;

        if (isGraceReplay)
        {
            if (!await IsWithinRotationGraceAsync(storedToken, now, ct))
            {
                await RevokeFamilyAsync(storedToken, now, ct);
                throw new UnauthorizedException(GenericFailureMessage);
            }

            // The grace replay is single-use, claimed atomically. Only a request racing the
            // claim at the same instant can lose here; it's refused without killing the
            // family (the winner's cookie stays valid), and it can't mint another token.
            // A later replay sees GraceReplayedAt already set and takes the revoke path above.
            if (!await _refreshTokens.TryClaimGraceReplayAsync(storedToken.Id, now, ct))
                throw new UnauthorizedException(GenericFailureMessage);
        }

        if (storedToken.ExpiresAt < now || !storedToken.User.IsActive)
            throw new UnauthorizedException(GenericFailureMessage);

        var rawToken = _jwtTokenService.GenerateRefreshToken();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = SecureTokens.Hash(rawToken),
            FamilyId = storedToken.FamilyId,
            // Rotation never extends the session — see RefreshTokenOptions.LifetimeDays.
            ExpiresAt = storedToken.ExpiresAt,
            CreatedAt = now
        };

        if (!isGraceReplay && !await _refreshTokens.TryClaimRotationAsync(storedToken.Id, replacement.TokenHash, now, ct))
        {
            // Lost a race with a concurrent rotation of this same cookie (e.g. two tabs at
            // once). That request owns the rotation; this one may only use the token's single
            // grace replay — so at most two tokens can ever descend from one cookie.
            if (!await _refreshTokens.TryClaimGraceReplayAsync(storedToken.Id, now, ct))
                throw new UnauthorizedException(GenericFailureMessage);
            isGraceReplay = true;
        }

        // In the grace case the presented token was already rotated; leave its original
        // ReplacedByTokenHash alone and just issue a sibling.
        await _refreshTokens.AddAsync(replacement, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // A grace sibling joins a family that reuse detection may have revoked between our
        // eligibility check and this insert. If nothing else in the family is still live,
        // the session was killed — so kill the sibling too rather than resurrect it.
        if (isGraceReplay && await _refreshTokens.CountOtherActiveInFamilyAsync(storedToken.FamilyId, replacement.Id, ct) == 0)
        {
            await _refreshTokens.RevokeFamilyAsync(storedToken.FamilyId, now, ct);
            throw new UnauthorizedException(GenericFailureMessage);
        }

        var user = storedToken.User;
        return new RefreshTokenResponseDto
        {
            AccessToken = _jwtTokenService.GenerateAccessToken(user, storedToken.FamilyId),
            ExpiresIn = _jwtTokenService.AccessTokenExpirySeconds,
            RefreshToken = rawToken,
            RefreshTokenExpiresAt = replacement.ExpiresAt,
            User = new UserSummaryDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = user.UserRoles?.Select(r => r.Role.Name).ToList() ?? new List<string>()
            }
        };
    }

    /// <summary>
    /// True when this revoked token was *rotated* (not killed) moments ago and its
    /// replacement is still live — the benign "two tabs refreshed at once" race. A family
    /// already killed by reuse detection has no live replacement, so it never qualifies.
    /// </summary>
    private async Task<bool> IsWithinRotationGraceAsync(RefreshToken token, DateTimeOffset now, CancellationToken ct)
    {
        if (token.ReplacedByTokenHash is null || token.RevokedAt is null)
            return false;

        // Already used its one replay — a further presentation is no longer the benign
        // two-tabs race, so the family-revoke path applies.
        if (token.GraceReplayedAt is not null)
            return false;

        if (now - token.RevokedAt.Value > TimeSpan.FromSeconds(_options.ReuseGraceSeconds))
            return false;

        var replacement = await _refreshTokens.GetByHashAsync(token.ReplacedByTokenHash, ct);
        return replacement is { IsRevoked: false };
    }

    private async Task RevokeFamilyAsync(RefreshToken token, DateTimeOffset now, CancellationToken ct)
    {
        // One set-based UPDATE, so it catches every member committed by the time it runs
        // (a load-then-loop revoke could miss a token inserted in between).
        var revoked = await _refreshTokens.RevokeFamilyAsync(token.FamilyId, now, ct);

        // Only worth a warning when there was something left to kill — replaying a token
        // from an already-ended session is just noise.
        if (revoked > 0)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoked {Count} active token(s) in family {FamilyId}",
                token.UserId, revoked, token.FamilyId);
        }
    }
}
