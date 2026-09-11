namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using Microsoft.EntityFrameworkCore;

public class ReportsRepository : IReportsRepository
{
    private readonly CrmDbContext _context;

    public ReportsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<(int TotalDonorsContacted, List<ManagerContactedDto> ByManager)> GetDonorsContactedAsync(
        DateTime startUtc,
        DateTime endUtc,
        Guid? relationshipManagerId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InteractionLogs
            .AsNoTracking()
            .Where(i => i.CreatedAt >= startUtc && i.CreatedAt <= endUtc);

        if (relationshipManagerId.HasValue)
            query = query.Where(i => i.Donor.RelationshipManagerId == relationshipManagerId.Value);

        // Project the (small, date-bounded) row set once and aggregate in memory below.
        // A COUNT(DISTINCT donor_id) alongside a plain COUNT(*) in the same GroupBy
        // projection is a translation pattern that's been unreliable across EF Core
        // provider versions, so we avoid depending on it here.
        var rows = await query
            .Select(i => new
            {
                i.DonorId,
                ManagerId = i.Donor.RelationshipManagerId,
                ManagerFirstName = i.Donor.RelationshipManager!.FirstName,
                ManagerLastName = i.Donor.RelationshipManager!.LastName
            })
            .ToListAsync(cancellationToken);

        // COUNT(DISTINCT donor_id) across ALL matching interactions — independent of the
        // per-manager breakdown below, so it can never be a (double-counting) sum of it.
        var totalDonorsContacted = rows.Select(r => r.DonorId).Distinct().Count();

        var byManager = rows
            .Where(r => r.ManagerId.HasValue)
            .GroupBy(r => r.ManagerId!.Value)
            .Select(g => new ManagerContactedDto
            {
                Manager = new ReportManagerDto
                {
                    Id = g.Key,
                    FullName = $"{g.First().ManagerFirstName} {g.First().ManagerLastName}"
                },
                DonorsContacted = g.Select(r => r.DonorId).Distinct().Count(),
                TotalInteractions = g.Count()
            })
            .OrderByDescending(m => m.DonorsContacted)
            .ToList();

        return (totalDonorsContacted, byManager);
    }
}
