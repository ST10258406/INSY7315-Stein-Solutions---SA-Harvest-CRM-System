using CRM.Application.Common.Interfaces;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var token = await _refreshTokens.GetByTokenAsync(request.RefreshToken, ct);

        // If it doesn't exist or is already revoked, do nothing —
        // still return success either way. Don't throw NotFoundException
        // here, that would leak whether the token was ever valid.
        if (token is not null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
