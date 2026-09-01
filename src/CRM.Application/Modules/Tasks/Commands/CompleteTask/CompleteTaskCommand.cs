namespace CRM.Application.Modules.Tasks.Commands.CompleteTask;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>POST /api/v1/tasks/{id}/complete — any authenticated user, not just the assignee.</summary>
public class CompleteTaskCommand : IRequest<TaskDto>, IAuditableCommand
{
    public Guid Id { get; set; }

    public string EntityType => "DonorTask";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
