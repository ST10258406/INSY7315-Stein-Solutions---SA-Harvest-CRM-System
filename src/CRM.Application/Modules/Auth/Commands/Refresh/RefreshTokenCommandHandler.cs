using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IRefreshTokenRepository refreshTokens, IJwtTokenService jwtTokenService)
    {
        _refreshTokens = refreshTokens;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var storedToken = await _refreshTokens.GetByTokenWithUserAndRolesAsync(request.RefreshToken, ct);

        // Same generic 401 for "doesn't exist", "revoked", and "expired" —
        // don't tell the caller which case it was.
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Refresh token is invalid or expired.");

        var newAccessToken = _jwtTokenService.GenerateAccessToken(storedToken.User);

        return new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            ExpiresIn = _jwtTokenService.AccessTokenExpirySeconds
        };
    }
}
