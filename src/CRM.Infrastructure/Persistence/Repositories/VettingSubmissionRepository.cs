namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class VettingSubmissionRepository : IVettingSubmissionRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public VettingSubmissionRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<string> GetNextFormReferenceAsync(CancellationToken cancellationToken = default)
    {
        // Only ever DateTime.UtcNow.Year — never user input — so building an identifier from it is safe.
        var year = DateTime.UtcNow.Year;
        var sequenceName = $"vetting_reference_seq_{year}";

        // InMemory (CRM.API.Tests) can't run raw SQL. This count+1 fallback is NOT
        // concurrency-safe and must never run against Postgres; same as DonorRepository.
        if (!_context.Database.IsNpgsql())
        {
            var count = await _context.VettingSubmissions.CountAsync(cancellationToken);
            return $"VET-{year}-{(count + 1):D4}";
        }

        long next;
        try
        {
            next = await NextValAsync(sequenceName, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") // undefined_table: first reference of the year
        {
            // IF NOT EXISTS makes a concurrent first-of-year create safe. Identifiers can't be parameters.
#pragma warning disable EF1002
            await _context.Database.ExecuteSqlRawAsync(
                $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1", cancellationToken);
#pragma warning restore EF1002

            next = await NextValAsync(sequenceName, cancellationToken);
        }

        return $"VET-{year}-{next:D4}";
    }

    private async Task<long> NextValAsync(string sequenceName, CancellationToken cancellationToken)
        => await _context.Database
            .SqlQuery<long>($"SELECT nextval({sequenceName}) AS \"Value\"")
            .SingleAsync(cancellationToken);

    public Task AddAsync(VettingSubmission submission, CancellationToken cancellationToken = default)
    {
        _context.VettingSubmissions.Add(submission);
        return Task.CompletedTask;
    }

    public Task<VettingSubmission?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked — feeds PATCH / submit. Only active flags: inactive ones are history, never touched again.
        => _context.VettingSubmissions
            .Include(s => s.Details)
            .Include(s => s.Flags.Where(f => f.IsActive))
            .Include(s => s.Photos)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<VettingSubmissionDto?> GetDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.VettingSubmissions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ProjectTo<VettingSubmissionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Guid?> GetOfficerIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.VettingSubmissions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => (Guid?)s.OfficerId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryRecordDecisionAsync(
        Guid id, VettingApprovalState newState, VettingDecision decision, string? reason, CancellationToken cancellationToken = default)
    {
        // InMemory (CRM.API.Tests) has no ExecuteUpdate. This read-then-write is NOT safe under
        // concurrency and must never run against Postgres; same trade-off as DonorRepository.
        if (!_context.Database.IsRelational())
        {
            var submission = await _context.VettingSubmissions
                .FirstOrDefaultAsync(s => s.Id == id && s.ApprovalState == VettingApprovalState.Ready, cancellationToken);
            if (submission is null) return false;
            submission.ApprovalState = newState;
            submission.AdminDecision = decision;
            submission.DecisionReason = reason;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // One statement: Postgres row-locks the match, so a second concurrent decider re-checks the
        // WHERE after the first commits and updates 0 rows. ExecuteUpdate skips the interceptor, hence updated_at.
        var rows = await _context.VettingSubmissions
            .Where(s => s.Id == id && s.ApprovalState == VettingApprovalState.Ready)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.ApprovalState, newState)
                .SetProperty(s => s.AdminDecision, decision)
                .SetProperty(s => s.DecisionReason, reason)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow), cancellationToken);
        return rows == 1;
    }

    public async Task<(List<VettingSubmissionListItemDto> Items, int TotalCount)> SearchAsync(
        VettingSubmissionSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var query = _context.VettingSubmissions.AsNoTracking().AsQueryable();

        if (criteria.ApprovalState.HasValue)
            query = query.Where(s => s.ApprovalState == criteria.ApprovalState.Value);
        if (criteria.DonorId.HasValue)
            query = query.Where(s => s.DonorId == criteria.DonorId.Value);
        if (criteria.OfficerId.HasValue)
            query = query.Where(s => s.OfficerId == criteria.OfficerId.Value);
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.ToLower();
            query = query.Where(s => s.FormReference.ToLower().Contains(search) || s.Donor.CompanyName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.FormReference)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ProjectTo<VettingSubmissionListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
