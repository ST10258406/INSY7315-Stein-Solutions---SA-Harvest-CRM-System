namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities.Vetting;
using Microsoft.EntityFrameworkCore;

public class VettingSiteVisitRepository : IVettingSiteVisitRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public VettingSiteVisitRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<VettingSiteVisitDto>> GetByDateAsync(DateOnly date, Guid? officerId, CancellationToken cancellationToken = default)
    {
        var query = _context.VettingSiteVisits.AsNoTracking().Where(v => v.ScheduledDate == date);

        if (officerId.HasValue)
            query = query.Where(v => v.OfficerId == officerId.Value);

        return query
            .OrderBy(v => v.ScheduledTime)
            .ProjectTo<VettingSiteVisitDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    public Task<VettingSiteVisitDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.VettingSiteVisits
            .AsNoTracking()
            .Where(v => v.Id == id)
            .ProjectTo<VettingSiteVisitDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(VettingSiteVisit visit, CancellationToken cancellationToken = default)
    {
        _context.VettingSiteVisits.Add(visit);
        return Task.CompletedTask;
    }

    public Task<VettingSiteVisit?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked (no AsNoTracking) — feeds a write.
        => _context.VettingSiteVisits.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
}
