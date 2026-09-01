namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class TaskRepository : ITaskRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public TaskRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(List<TaskDto> Items, int TotalCount)> GetTasksAsync(
        TaskSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.DonorTasks.AsNoTracking().AsQueryable();

        if (criteria.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == criteria.AssignedToUserId.Value);

        if (criteria.DonorId.HasValue)
            query = query.Where(t => t.DonorId == criteria.DonorId.Value);

        if (criteria.IsCompleted.HasValue)
            query = query.Where(t => t.IsCompleted == criteria.IsCompleted.Value);

        if (criteria.DueBefore.HasValue)
            query = query.Where(t => t.DueDate <= criteria.DueBefore.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task AddAsync(DonorTask task, CancellationToken cancellationToken = default)
    {
        _context.DonorTasks.Add(task);
        return Task.CompletedTask;
    }

    public Task<DonorTask?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked (no AsNoTracking) — feeds a write.
        => _context.DonorTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TaskDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.DonorTasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .ProjectTo<TaskDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
}
