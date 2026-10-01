using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return;

        var token = await _refreshTokens.GetByHashWithUserAndRolesAsync(SecureTokens.Hash(request.RefreshToken), ct);

        // Unknown, already revoked, or someone else's token: do nothing and still succeed —
        // don't throw NotFoundException, that would leak whether the token was ever valid,
        // and never let one user end another user's session (F-21).
        if (token is null || token.UserId != _currentUserService.GetCurrentUserId())
            return;

        // End this session's whole rotation chain, not just the one link presented.
        var now = DateTimeOffset.UtcNow;
        var family = await _refreshTokens.GetActiveByFamilyIdAsync(token.FamilyId, ct);
        foreach (var member in family)
            member.Revoke(now);

        if (family.Count > 0)
            await _unitOfWork.SaveChangesAsync(ct);
    }
}
