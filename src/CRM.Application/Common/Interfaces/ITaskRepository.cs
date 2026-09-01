namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;

public interface ITaskRepository
{
    /// <summary>
    /// Filtered, paginated task list (soonest due first) plus the total matching row count
    /// (counted before pagination). Backs both GET /tasks and GET /donors/{id}/tasks.
    /// </summary>
    Task<(List<TaskDto> Items, int TotalCount)> GetTasksAsync(
        TaskSearchCriteria criteria, CancellationToken cancellationToken = default);
}
