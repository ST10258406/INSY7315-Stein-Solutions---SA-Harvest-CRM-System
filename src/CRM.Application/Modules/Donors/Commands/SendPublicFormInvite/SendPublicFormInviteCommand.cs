namespace CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;

// IAuditableCommand → AuditBehaviour writes the audit_logs "Created" row for the
// EmailLog. Never reached on a failed send (the handler throws
// EmailDeliveryException before returning), same discipline as SendDonorEmailCommand.
public class SendPublicFormInviteCommand : IRequest<SendPublicFormInviteResponseDto>, IAuditableCommand
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Built by the controller from configuration (Frontend:BaseUrl) — Application
    // stays free of IConfiguration/ASP.NET Core, same reasoning as every other
    // Clean Architecture boundary in this codebase.
    public string PublicFormUrl { get; set; } = string.Empty;

    public string EntityType => "EmailLog";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
