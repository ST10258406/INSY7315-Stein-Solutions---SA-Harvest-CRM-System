using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Extensions;

namespace CRM.Application.Modules.Donors.Queries.GetDonors;

public class GetDonorsQueryHandler : IRequestHandler<GetDonorsQuery, PaginatedResult<DonorListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetDonorsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<DonorListItemDto>> Handle(GetDonorsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Donors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(d => EF.Functions.ILike(d.CompanyName, $"%{request.Search}%"));

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<DonorStatus>(request.Status, true, out var status))
            query = query.Where(d => d.Status == status);

        if (request.CompanyTypeId.HasValue)
            query = query.Where(d => d.CompanyTypeId == request.CompanyTypeId.Value);

        if (!string.IsNullOrWhiteSpace(request.RegionCode))
            query = query.Where(d => d.OperationalRegions.Any(r => r.OperationalRegion.Code == request.RegionCode));

        if (request.DonationTypeId.HasValue)
            query = query.Where(d => d.DonationTypes.Any(dt => dt.DonationTypeId == request.DonationTypeId.Value));

        if (request.DonationFrequencyId.HasValue)
            query = query.Where(d => d.DonationFrequencyId == request.DonationFrequencyId.Value);

        if (request.RelationshipManagerId.HasValue)
            query = query.Where(d => d.RelationshipManagerId == request.RelationshipManagerId.Value);

        if (request.FollowUpBefore.HasValue)
        {
            var followUpBefore = request.FollowUpBefore.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(d => d.FollowUpDate != null && d.FollowUpDate <= followUpBefore);
        }

        // Whitelist sortBy against known fields - never build dynamic LINQ from an arbitrary string.
        var sortDir = request.SortDir.ToLowerInvariant();
        query = (request.SortBy?.ToLowerInvariant(), sortDir) switch
        {
            ("followupdate", "desc") => query.OrderByDescending(d => d.FollowUpDate),
            ("followupdate", _) => query.OrderBy(d => d.FollowUpDate),
            ("companyname", "desc") => query.OrderByDescending(d => d.CompanyName),
            ("companyname", _) => query.OrderBy(d => d.CompanyName),
            ("lastinteractiondate", "desc") => query.OrderByDescending(d =>
                d.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()),
            ("lastinteractiondate", _) => query.OrderBy(d =>
                d.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()),
            (_, "desc") => query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<DonorListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return PaginatedResult<DonorListItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
