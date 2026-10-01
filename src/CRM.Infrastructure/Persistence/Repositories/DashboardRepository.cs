namespace CRM.Infrastructure.Persistence.Repositories;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Constants;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class DashboardRepository : IDashboardRepository
{
    private readonly CrmDbContext _context;

    public DashboardRepository(CrmDbContext context)
    {
        _context = context;
    }

    public Task<int> GetTotalDonorsCountAsync(CancellationToken cancellationToken = default)
        // Donor has no soft-delete flag in the current schema, so every row here is already
        // "not deleted" — this is a no-op filter until one is added.
        => _context.Donors.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> GetActiveDonorsCountAsync(CancellationToken cancellationToken = default)
        => _context.Donors.AsNoTracking().CountAsync(d => d.Status == DonorStatus.Active, cancellationToken);

    public Task<int> GetPendingApprovalsCountAsync(CancellationToken cancellationToken = default)
        => _context.DonorApprovals.AsNoTracking().CountAsync(a => a.Status == ApprovalStatus.Pending, cancellationToken);

    public Task<int> GetOpenTaskCountAsync(Guid assignedToUserId, CancellationToken cancellationToken = default)
        => _context.DonorTasks.AsNoTracking()
            .CountAsync(t => t.AssignedToUserId == assignedToUserId && !t.IsCompleted, cancellationToken);

    public Task<int> GetOverdueFollowUpCountAsync(
        Guid relationshipManagerId, DateTime asOfUtc, CancellationToken cancellationToken = default)
        => _context.Donors.AsNoTracking()
            .CountAsync(d => d.RelationshipManagerId == relationshipManagerId
                && d.FollowUpDate != null && d.FollowUpDate < asOfUtc, cancellationToken);

    public async Task<List<ManagerActivityRow>> GetDonorsContactedByUserAsync(
        DateTime startUtc, DateTime endUtc, int take, CancellationToken cancellationToken = default)
    {
        // Project the (small, date-bounded) row set once and aggregate in memory: a grouped
        // COUNT(DISTINCT) into a constructor projection doesn't translate reliably in EF Core
        // (same reason ReportsRepository.GetDonorsContactedAsync does it this way).
        var rows = await _context.InteractionLogs
            .AsNoTracking()
            .Where(i => i.CreatedAt >= startUtc && i.CreatedAt < endUtc)
            // System actors (e.g. the public-form submitter) aren't team members.
            .Where(i => !i.CreatedByUser.Email.ToLower().EndsWith(SystemUsers.ReservedEmailDomain))
            .Select(i => new
            {
                i.DonorId,
                UserId = i.CreatedByUserId,
                i.CreatedByUser.FirstName,
                i.CreatedByUser.LastName
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.UserId)
            .Select(g => new ManagerActivityRow(
                g.Key,
                g.First().FirstName,
                g.First().LastName,
                g.Select(r => r.DonorId).Distinct().Count()))
            .OrderByDescending(r => r.DonorsContacted)
            .ThenBy(r => r.FirstName)
            .Take(take)
            .ToList();
    }

    public Task<int> GetDonorsContactedCountAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
        => _context.InteractionLogs
            .AsNoTracking()
            .Where(i => i.CreatedAt >= startUtc && i.CreatedAt < endUtc)
            .Select(i => i.DonorId)
            .Distinct()
            .CountAsync(cancellationToken);
}
