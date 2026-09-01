namespace CRM.Application.Modules.Tasks.Commands.UpdateTask;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UpdateTaskCommand : IRequest<TaskDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public string EntityType => "DonorTask";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
