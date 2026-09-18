namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;

using CRM.Application.Modules.PublicDonors.Dtos;
using MediatR;

// Deliberately does NOT implement IAuditableCommand. AuditBehaviour unconditionally
// calls ICurrentUserService.GetCurrentUserId() for any IAuditableCommand, which
// throws UnauthorizedException when there's no authenticated caller — exactly the
// case here. The handler writes its own audit_logs row instead (UserId = null —
// there genuinely is no user to attribute this to), the one deliberate exception
// the copilot-instructions.md PR checklist calls out for this exact endpoint.
public class SubmitPublicDonorCommand : IRequest<SubmitPublicDonorResponseDto>
{
    public SubmitPublicDonorRequest Request { get; set; } = null!;

    /// <summary>Captured for the manual audit_logs row — see remarks above.</summary>
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
