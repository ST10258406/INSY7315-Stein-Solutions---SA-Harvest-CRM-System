using MediatR;

namespace CRM.Application.Modules.Auth.Commands.ResetPassword;

public class ResetPasswordResponseDto
{
    public string Message { get; set; } = default!;
}

public record ResetPasswordCommand(
    string Token,
    string Email,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordResponseDto>;
