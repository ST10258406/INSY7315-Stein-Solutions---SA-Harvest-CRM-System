using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Application.Modules.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ResetPasswordCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResetPasswordResponseDto> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // Tracked (not read-only) — we're updating this entity.
        var user = await _users.GetByEmailAsync(request.Email, ct);

        // Hash-and-compare in constant time (SecureTokens.Matches) — the stored value is a
        // hash, and FixedTimeEquals means timing reveals nothing about a near-miss guess.
        if (user is null
            || !SecureTokens.Matches(request.Token, user.PasswordResetTokenHash)
            || user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Token", "Token expired or invalid.") });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        // Completing a reset proves control of the mailbox, so any forced-change flag is satisfied.
        user.MustChangePassword = false;

        // Single-use: burn the token immediately so it can't be replayed.
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;

        // Force re-login everywhere — kill every existing session.
        var activeTokens = await _refreshTokens.GetActiveByUserIdAsync(user.Id, ct);

        foreach (var rt in activeTokens)
        {
            rt.Revoke(DateTimeOffset.UtcNow);
        }

        // The user mutation and the token revocations are tracked by the same scoped
        // persistence context, so this single call commits both in one transaction.
        await _unitOfWork.SaveChangesAsync(ct);

        return new ResetPasswordResponseDto
        {
            Message = "Password has been reset successfully."
        };
    }
}
