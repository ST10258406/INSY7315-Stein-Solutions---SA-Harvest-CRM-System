using System.Security.Cryptography;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Modules.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseDto>
{
    private const string GenericMessage = "If this email address exists, a reset link has been sent.";
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<ForgotPasswordResponseDto> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // IMPORTANT: do this DB lookup and the branching below unconditionally
        // for every request, existing user or not. Do NOT short-circuit with
        // an early "return genericMessage" before the query — that would make
        // a non-existent-email request measurably faster than a real one,
        // which is exactly the timing leak this endpoint is designed to avoid.

        if (user is not null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            await _context.SaveChangesAsync(ct);

            var resetLink = $"http://localhost:5173/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

            await _emailService.SendAsync(
                to: user.Email,
                subject: "Reset your SA Harvest CRM password",
                htmlBody: BuildResetEmailBody(user.FirstName, resetLink));
        }

        return new ForgotPasswordResponseDto { Message = GenericMessage };
    }

    private static string BuildResetEmailBody(string firstName, string resetLink) =>
        $"<p>Hi {firstName},</p><p>Click below to reset your password. This link expires in 1 hour.</p><p><a href=\"{resetLink}\">Reset Password</a></p><p>If you didn't request this, ignore this email.</p>";
}
