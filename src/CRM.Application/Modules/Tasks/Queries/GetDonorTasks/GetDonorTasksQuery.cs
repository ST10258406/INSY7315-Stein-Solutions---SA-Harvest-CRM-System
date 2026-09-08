namespace CRM.Application.Modules.Tasks.Queries.GetDonorTasks;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/donors/{id}/tasks — every task for one donor. <see cref="IsCompleted"/> is a
/// tri-state string: "false" | "true" | "all" (default "all").
/// </summary>
public record GetDonorTasksQuery : PaginationParams, IRequest<PaginatedResult<TaskDto>>
{
    public Guid DonorId { get; init; }
    public string IsCompleted { get; init; } = "all";
}
