namespace CRM.Application.Modules.Tasks.Queries.GetDonorTasks;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using MediatR;

public class GetDonorTasksQueryHandler : IRequestHandler<GetDonorTasksQuery, PaginatedResult<TaskDto>>
{
    private readonly IDonorRepository _donors;
    private readonly ITaskRepository _tasks;

    public GetDonorTasksQueryHandler(IDonorRepository donors, ITaskRepository tasks)
    {
        _donors = donors;
        _tasks = tasks;
    }

    public async Task<PaginatedResult<TaskDto>> Handle(GetDonorTasksQuery request, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(request.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), request.DonorId);

        var criteria = new TaskSearchCriteria
        {
            DonorId = request.DonorId,
            IsCompleted = ParseTriState(request.IsCompleted),
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (items, totalCount) = await _tasks.GetTasksAsync(criteria, cancellationToken);

        return PaginatedResult<TaskDto>.Create(items, request.Page, request.PageSize, totalCount);
    }

    // "all" (or anything unrecognised the validator already rejected) => no completion filter.
    private static bool? ParseTriState(string value) => value?.ToLowerInvariant() switch
    {
        "true" => true,
        "false" => false,
        _ => null
    };
}
