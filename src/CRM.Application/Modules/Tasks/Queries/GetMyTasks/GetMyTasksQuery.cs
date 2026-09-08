namespace CRM.Application.Modules.Tasks.Queries.GetMyTasks;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/tasks — the current user's assigned tasks only (assignee comes from the JWT,
/// never the request). <see cref="IsCompleted"/> defaults to false so open tasks show first.
/// </summary>
public record GetMyTasksQuery : PaginationParams, IRequest<PaginatedResult<TaskDto>>
{
    public bool IsCompleted { get; init; } = false;
    public DateOnly? DueBefore { get; init; }
    public Guid? DonorId { get; init; }
}
