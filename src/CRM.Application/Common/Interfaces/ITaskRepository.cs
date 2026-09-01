namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;

public interface ITaskRepository
{
    /// <summary>
    /// Filtered, paginated task list (soonest due first) plus the total matching row count
    /// (counted before pagination). Backs both GET /tasks and GET /donors/{id}/tasks.
    /// </summary>
    Task<(List<TaskDto> Items, int TotalCount)> GetTasksAsync(
        TaskSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>Stages a new task for insert. Commit with <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task AddAsync(DonorTask task, CancellationToken cancellationToken = default);

    /// <summary>Single task projected to <see cref="TaskDto"/>, or null. Used to re-read after a write.</summary>
    Task<TaskDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
