namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class VettingOfficerRepository : IVettingOfficerRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public VettingOfficerRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<VettingOfficerDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _context.VettingOfficers
            .AsNoTracking()
            .OrderBy(o => o.FullName)
            .ProjectTo<VettingOfficerDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public Task<VettingOfficerDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.VettingOfficers
            .AsNoTracking()
            .Where(o => o.Id == id)
            .ProjectTo<VettingOfficerDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.VettingOfficers.AsNoTracking().AnyAsync(o => o.Id == id, cancellationToken);

    public Task AddAsync(VettingOfficer officer, CancellationToken cancellationToken = default)
    {
        _context.VettingOfficers.Add(officer);
        return Task.CompletedTask;
    }

    public Task<VettingOfficer?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked (no AsNoTracking) — feeds a write.
        => _context.VettingOfficers.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task RecalculateWorkloadAsync(Guid officerId, CancellationToken cancellationToken = default)
    {
        // Row lock until the caller's transaction ends: a concurrent recalculation waits here, then
        // counts after we commit (READ COMMITTED), so it sees our row. InMemory has no locks or raw SQL.
        var officer = _context.Database.IsNpgsql()
            ? await _context.VettingOfficers
                .FromSqlInterpolated($"SELECT * FROM vetting_officers WHERE id = {officerId} FOR UPDATE")
                .FirstAsync(cancellationToken)
            : await _context.VettingOfficers.FirstAsync(o => o.Id == officerId, cancellationToken);

        var openVisits = await _context.VettingSiteVisits
            .CountAsync(v => v.OfficerId == officerId && v.VisitStatus != SiteVisitStatus.Completed, cancellationToken);
        var openSubmissions = await _context.VettingSubmissions
            .CountAsync(s => s.OfficerId == officerId
                && (s.ApprovalState == VettingApprovalState.New
                    || s.ApprovalState == VettingApprovalState.Ready
                    || s.ApprovalState == VettingApprovalState.Returned), cancellationToken);

        officer.WorkloadCount = openVisits + openSubmissions;
    }
}
