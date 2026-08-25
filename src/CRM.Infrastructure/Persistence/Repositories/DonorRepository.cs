namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class DonorRepository : IDonorRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public DonorRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task AddAsync(Donor donor, CancellationToken cancellationToken = default)
    {
        _context.Donors.Add(donor);
        return Task.CompletedTask;
    }

    public Task AddApprovalAsync(DonorApproval approval, CancellationToken cancellationToken = default)
    {
        _context.DonorApprovals.Add(approval);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Donors.AsNoTracking().AnyAsync(d => d.Id == id, cancellationToken);

    public Task<Donor?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked query (no AsNoTracking) — this feeds a write, EF needs to track
        // changes so SaveChangesAsync knows what to update.
        => _context.Donors
            .Include(d => d.Contacts)
            .Include(d => d.LegalAddress)
            .Include(d => d.OperationalRegions)
            .Include(d => d.DonationTypes)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<(List<DonorListItemDto> Items, int TotalCount)> SearchAsync(
        DonorSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.Donors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.ToLower();
            query = query.Where(d => d.CompanyName.ToLower().Contains(search));
        }

        if (criteria.Status.HasValue)
            query = query.Where(d => d.Status == criteria.Status.Value);

        if (criteria.CompanyTypeId.HasValue)
            query = query.Where(d => d.CompanyTypeId == criteria.CompanyTypeId.Value);

        if (!string.IsNullOrWhiteSpace(criteria.RegionCode))
            query = query.Where(d => d.OperationalRegions.Any(r => r.OperationalRegion.Code == criteria.RegionCode));

        if (criteria.DonationTypeId.HasValue)
            query = query.Where(d => d.DonationTypes.Any(dt => dt.DonationTypeId == criteria.DonationTypeId.Value));

        if (criteria.DonationFrequencyId.HasValue)
            query = query.Where(d => d.DonationFrequencyId == criteria.DonationFrequencyId.Value);

        if (criteria.RelationshipManagerId.HasValue)
            query = query.Where(d => d.RelationshipManagerId == criteria.RelationshipManagerId.Value);

        if (criteria.FollowUpBefore.HasValue)
        {
            var followUpBefore = criteria.FollowUpBefore.Value;
            query = query.Where(d => d.FollowUpDate != null && d.FollowUpDate <= followUpBefore);
        }

        // Whitelist sortBy against known fields - never build dynamic LINQ from an arbitrary string.
        var sortDir = criteria.SortDir.ToLowerInvariant();
        query = (criteria.SortBy?.ToLowerInvariant(), sortDir) switch
        {
            ("followupdate", "desc") => query.OrderByDescending(d => d.FollowUpDate),
            ("followupdate", _) => query.OrderBy(d => d.FollowUpDate),
            ("companyname", "desc") => query.OrderByDescending(d => d.CompanyName),
            ("companyname", _) => query.OrderBy(d => d.CompanyName),
            ("lastinteractiondate", "desc") => query.OrderByDescending(d =>
                d.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()),
            ("lastinteractiondate", _) => query.OrderBy(d =>
                d.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()),
            ("createdat", "asc") => query.OrderBy(d => d.CreatedAt),
            ("createdat", _) => query.OrderByDescending(d => d.CreatedAt),
            (_, "desc") => query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ProjectTo<DonorListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<DonorDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Donors
            .AsNoTracking()
            .Where(d => d.Id == id)
            .ProjectTo<DonorDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
}
