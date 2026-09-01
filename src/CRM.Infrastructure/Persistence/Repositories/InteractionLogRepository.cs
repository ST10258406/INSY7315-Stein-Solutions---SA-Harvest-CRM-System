namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class InteractionLogRepository : IInteractionLogRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public InteractionLogRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task AddAsync(InteractionLog log, CancellationToken cancellationToken = default)
    {
        _context.InteractionLogs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<(List<InteractionLogDto> Items, int TotalCount)> GetByDonorAsync(
        Guid donorId,
        InteractionType? interactionType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InteractionLogs
            .AsNoTracking()
            .Where(i => i.DonorId == donorId);

        if (interactionType.HasValue)
            query = query.Where(i => i.InteractionType == interactionType.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<InteractionLogDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<InteractionLogDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.InteractionLogs
            .AsNoTracking()
            .Where(i => i.Id == id)
            .ProjectTo<InteractionLogDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
}
