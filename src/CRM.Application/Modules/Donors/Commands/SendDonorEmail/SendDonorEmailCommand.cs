namespace CRM.Application.Modules.Donors.Commands.SendDonorEmail;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Enums;
using MediatR;

// IAuditableCommand → AuditBehaviour writes the audit_logs "Created" row for the
// InteractionLog, exactly like LogInteractionCommand. Never reached on a failed
// send (the handler throws EmailDeliveryException before returning), so a failed
// send correctly produces no audit row either.
public class SendDonorEmailCommand : IRequest<InteractionLogDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string EntityType => "InteractionLog";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
