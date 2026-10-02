using CRM.Application.Modules.Auth.Dtos;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.ForgotPassword;

// Deliberately a plain class bound by the controller from ForgotPasswordRequest,
// not the direct [FromBody] action parameter (see AuthController.ForgotPassword).
// ResetPasswordUrl must never be bindable from the request body — accepting it
// from the client would let an attacker redirect a real reset token to an
// attacker-controlled domain (a phishing / account-takeover vector), so the
// wire type (ForgotPasswordRequest) doesn't even have that field.
public class ForgotPasswordCommand : IRequest<ForgotPasswordResponseDto>
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Built by the controller from configuration (Frontend:BaseUrl).</summary>
    public string ResetPasswordUrl { get; set; } = string.Empty;
}
