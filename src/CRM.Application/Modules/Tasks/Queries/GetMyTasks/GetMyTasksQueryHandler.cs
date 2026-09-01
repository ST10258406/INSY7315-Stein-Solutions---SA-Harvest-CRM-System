namespace CRM.Application.Modules.Tasks.Queries.GetMyTasks;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using MediatR;

public class GetMyTasksQueryHandler : IRequestHandler<GetMyTasksQuery, PaginatedResult<TaskDto>>
{
    private readonly ITaskRepository _tasks;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTasksQueryHandler(ITaskRepository tasks, ICurrentUserService currentUserService)
    {
        _tasks = tasks;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<TaskDto>> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        var criteria = new TaskSearchCriteria
        {
            // Assignee is always the caller — never sourced from the request.
            AssignedToUserId = _currentUserService.GetCurrentUserId(),
            DonorId = request.DonorId,
            IsCompleted = request.IsCompleted,
            // Inclusive of the whole requested day.
            DueBefore = request.DueBefore?.ToDateTime(TimeOnly.MaxValue),
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (items, totalCount) = await _tasks.GetTasksAsync(criteria, cancellationToken);

        return PaginatedResult<TaskDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
