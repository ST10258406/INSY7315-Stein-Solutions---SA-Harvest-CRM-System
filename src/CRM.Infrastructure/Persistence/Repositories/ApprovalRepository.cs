namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public class ApprovalRepository : IApprovalRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public ApprovalRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(List<ApprovalDto> Items, int TotalCount)> GetApprovalsAsync(
        ApprovalStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.DonorApprovals
            .AsNoTracking()
            .Where(a => a.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<ApprovalDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<DonorApproval?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        // Tracked (no AsNoTracking) with the donor — approve/reject mutate both rows.
        => _context.DonorApprovals
            .Include(a => a.Donor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
