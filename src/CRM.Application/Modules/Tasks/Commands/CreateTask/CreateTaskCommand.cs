namespace CRM.Application.Modules.Tasks.Commands.CreateTask;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Enums;
using MediatR;

// IAuditableCommand → AuditBehaviour writes the audit_logs "Created" row.
public class CreateTaskCommand : IRequest<TaskDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public DateOnly DueDate { get; set; }

    public string EntityType => "DonorTask";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
