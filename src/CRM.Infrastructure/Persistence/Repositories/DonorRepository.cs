namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<string> GetNextReferenceNumberAsync(CancellationToken cancellationToken = default)
    {
        // Only ever DateTime.UtcNow.Year, an int the caller can't influence — never
        // user input — so building an identifier from it below is safe.
        var year = DateTime.UtcNow.Year;
        var sequenceName = $"donor_reference_seq_{year}";

        // The nextval()-based path below needs a real Postgres connection — EF Core's
        // InMemory provider (used by the CRM.API.Tests WebApplicationFactory suite for
        // speed) can't execute raw SQL at all. This fallback is NOT concurrency-safe
        // (a classic "count + 1" race) and must never run against Postgres — it exists
        // solely so those in-process API tests can exercise CreateDonor/SubmitPublicDonor
        // end-to-end without a real database. Production always uses Npgsql.
        if (!_context.Database.IsNpgsql())
        {
            var count = await _context.Donors.CountAsync(cancellationToken);
            return $"DON-{year}-{(count + 1):D5}";
        }

        long next;
        try
        {
            next = await NextValAsync(sequenceName, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") // undefined_table: sequence doesn't exist yet
        {
            // Only ever happens on the first submission of a new calendar year.
            // IF NOT EXISTS makes this safe if two requests race here at once —
            // Postgres serializes the catalog write, so at most one create wins
            // and the other just finds it already there. A sequence/table name
            // can't be a bound parameter in any SQL dialect (identifiers aren't
            // parameterizable), so this one raw-SQL call is unavoidable — safe
            // per the comment above, since sequenceName is never user input.
#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync(
                $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1", cancellationToken);
#pragma warning restore EF1002

            next = await NextValAsync(sequenceName, cancellationToken);
        }

        return $"DON-{year}-{next:D5}";
    }

    // nextval()'s argument is a regclass, which accepts a plain text value, so
    // this can go through the parameterized SqlQuery (not SqlQueryRaw) — unlike
    // the CREATE SEQUENCE identifier above, this one doesn't need raw interpolation.
    private async Task<long> NextValAsync(string sequenceName, CancellationToken cancellationToken)
        => await _context.Database
            .SqlQuery<long>($"SELECT nextval({sequenceName}) AS \"Value\"")
            .SingleAsync(cancellationToken);
}
