using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Modules.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ResetPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResetPasswordResponseDto> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // Tracked (not AsNoTracking) — we're updating this entity.
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null
            || user.PasswordResetToken != request.Token
            || user.PasswordResetTokenExpiresAt is null
            || user.PasswordResetTokenExpiresAt < DateTimeOffset.UtcNow)
        {
            // Note: plain string comparison used for token here. Consider constant-time comparison in future for defense-in-depth against timing attacks.
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Token", "Token expired or invalid.") });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

        // Single-use: burn the token immediately so it can't be replayed.
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        // Force re-login everywhere — kill every existing session.
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var rt in activeTokens)
        {
            rt.IsRevoked = true;
        }

        await _context.SaveChangesAsync(ct);

        return new ResetPasswordResponseDto
        {
            Message = "Password has been reset successfully."
        };
    }
}
