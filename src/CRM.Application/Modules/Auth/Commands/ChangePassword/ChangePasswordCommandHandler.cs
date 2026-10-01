using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Application.Modules.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ChangePasswordCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();

        // Tracked — we're updating this entity.
        var user = await _users.GetByIdAsync(currentUserId, ct);

        if (user is null)
            throw new NotFoundException("User not found."); // shouldn't happen if the token is valid, but don't assume

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("CurrentPassword", "Current password is incorrect.") });
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("NewPassword", "New password cannot be the same as the current password.") });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.MustChangePassword = false;

        // A password change should end every *other* session — if the old password was
        // compromised, the attacker's sessions must not survive it (F-05). The caller's own
        // session (identified by its cookie) is kept so they aren't logged out mid-action.
        var currentHash = string.IsNullOrEmpty(request.CurrentRefreshToken)
            ? null
            : SecureTokens.Hash(request.CurrentRefreshToken);
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await _refreshTokens.GetActiveByUserIdAsync(user.Id, ct);
        var currentFamily = activeTokens.FirstOrDefault(rt => rt.TokenHash == currentHash)?.FamilyId;

        foreach (var rt in activeTokens.Where(rt => rt.FamilyId != currentFamily))
        {
            rt.Revoke(now);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
