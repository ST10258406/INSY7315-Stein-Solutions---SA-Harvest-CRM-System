namespace CRM.Application.Modules.Interactions.Commands.LogInteraction;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Enums;
using MediatR;

// IAuditableCommand → AuditBehaviour writes the audit_logs "Created" row; the handler
// never touches AuditLogs directly.
public class LogInteractionCommand : IRequest<InteractionLogDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public string InteractionType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }

    public string EntityType => "InteractionLog";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
