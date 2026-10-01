using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Application.Modules.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private const string GenericFailureMessage = "Invalid email or password.";

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly LoginLockoutOptions _lockout;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    // A fixed, precomputed hash used to verify against when there's no real user/hash to
    // check — keeps the expensive PBKDF2 verification on the same code path regardless of
    // whether the email exists or is active, so "unknown email", "deactivated account",
    // and "wrong password" aren't distinguishable by response timing.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(null!, "not-a-real-password-timing-guard");

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<LoginLockoutOptions> lockout,
        ILogger<LoginCommandHandler> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _lockout = lockout.Value;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        // Tracked — a failed attempt updates the lockout counter on this instance.
        var user = await _users.GetByEmailWithRolesAsync(request.Email, ct);
        var isKnownActiveUser = user is not null && user.IsActive;
        var now = DateTimeOffset.UtcNow;

        // Always verify against *some* hash — the real one for a known, active user, a
        // fixed dummy one otherwise — so this line runs on every request regardless of
        // which branch we're about to take. Skipping it for "no user"/"deactivated"/
        // "locked" would let those cases return faster than "wrong password" and leak
        // account state through timing despite sharing the same error message.
        var result = _passwordHasher.VerifyHashedPassword(
            user!, isKnownActiveUser ? user!.PasswordHash : DummyPasswordHash, request.Password);

        // Same generic failure for "no user", "deactivated user", "locked out" and "wrong
        // password" — do not let these branches produce different error messages. The
        // redundant `user is null` check lets the compiler narrow `user` to non-null
        // below (isKnownActiveUser already implies it).
        if (!isKnownActiveUser || user is null)
            throw LoginFailed(request.Email);

        // A locked account rejects even the correct password, and attempts during the lock
        // don't extend it (that would let an attacker keep a victim locked out forever at
        // the cost of one request per window).
        if (user.LockoutEndUtc > now)
            throw LoginFailed(request.Email);

        if (result == PasswordVerificationResult.Failed)
        {
            RecordFailedAttempt(user, now);
            await _unitOfWork.SaveChangesAsync(ct);
            throw LoginFailed(request.Email);
        }

        // Success clears any partial count and any expired lock.
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _refreshTokens.AddAsync(refreshToken, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = _jwtTokenService.AccessTokenExpirySeconds,
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

    private void RecordFailedAttempt(User user, DateTimeOffset now)
    {
        user.FailedLoginCount++;

        if (user.FailedLoginCount < _lockout.MaxFailedAttempts)
            return;

        // Reset the counter as the lock starts, so once it expires the account gets a fresh
        // MaxFailedAttempts tries rather than relocking on the next single mistake.
        user.LockoutEndUtc = now.AddMinutes(_lockout.LockoutMinutes);
        user.FailedLoginCount = 0;

        // Server-side only: names the account so an Admin knows whom to unlock.
        _logger.LogWarning(
            "User {UserId} locked out until {LockoutEndUtc:o} after {MaxFailedAttempts} consecutive failed logins",
            user.Id, user.LockoutEndUtc, _lockout.MaxFailedAttempts);
    }

    // Logged identically for every failure reason (no password, no raw email, no hint
    // whether the account exists) — the hashed email still lets repeated attempts
    // against one address be correlated. Replace with an audit-log event once the
    // audit plumbing from Sprint 7 #16 lands.
    private UnauthorizedException LoginFailed(string email)
    {
        _logger.LogWarning("Failed login attempt for email hash {EmailHash}", LogRedaction.HashEmail(email));
        return new UnauthorizedException(GenericFailureMessage);
    }
}
