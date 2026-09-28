using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Application.Modules.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
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
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        // Read-only lookup (the refresh token below is a separate entity being added).
        var user = await _users.GetByEmailWithRolesAsync(request.Email, ct);
        var isKnownActiveUser = user is not null && user.IsActive;

        // Always verify against *some* hash — the real one for a known, active user, a
        // fixed dummy one otherwise — so this line runs on every request regardless of
        // which branch we're about to take. Skipping it for "no user"/"deactivated"
        // would let those cases return faster than "wrong password" and leak account
        // state through timing despite sharing the same error message.
        var result = _passwordHasher.VerifyHashedPassword(
            user!, isKnownActiveUser ? user!.PasswordHash : DummyPasswordHash, request.Password);

        // Same generic failure for "no user", "deactivated user", and "wrong password" —
        // do not let these branches produce different error messages or timings. The
        // redundant `user is null` check lets the compiler narrow `user` to non-null
        // below (isKnownActiveUser already implies it).
        if (!isKnownActiveUser || result == PasswordVerificationResult.Failed || user is null)
            throw new UnauthorizedException("Invalid email or password.");

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
}
