namespace CRM.Application.Modules.Tasks.Commands.CompleteTask;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// POST /api/v1/tasks/{id}/complete — any authenticated user, not just the assignee.
/// Returns 204 No Content.
/// </summary>
public class CompleteTaskCommand : IRequest, IAuditableCommand
{
    public Guid Id { get; set; }

    public string EntityType => "DonorTask";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
