using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Modules.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        // Same generic 401 for "doesn't exist", "revoked", and "expired" —
        // don't tell the caller which case it was.
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Refresh token is invalid or expired.");

        var newAccessToken = _jwtTokenService.GenerateAccessToken(storedToken.User);

        return new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            ExpiresIn = 3600
        };
    }
}
