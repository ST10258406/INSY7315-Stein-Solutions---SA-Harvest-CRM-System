namespace CRM.Application.Modules.Tasks.Commands.ReopenTask;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>POST /api/v1/tasks/{id}/reopen — Procurement only (asymmetric with complete).</summary>
public class ReopenTaskCommand : IRequest<TaskDto>, IAuditableCommand
{
    public Guid Id { get; set; }

    public string EntityType => "DonorTask";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
