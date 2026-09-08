using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;

namespace CRM.Application.Modules.Donors.Queries.GetDonors;

public class GetDonorsQueryHandler : IRequestHandler<GetDonorsQuery, PaginatedResult<DonorListItemDto>>
{
    private readonly IDonorRepository _donors;

    public GetDonorsQueryHandler(IDonorRepository donors) => _donors = donors;

    public async Task<PaginatedResult<DonorListItemDto>> Handle(GetDonorsQuery request, CancellationToken cancellationToken)
    {
        // Status arrives as a raw string; an unparseable value applies no status filter,
        // matching the previous behaviour (GetDonorsQueryValidator rejects bad values first).
        DonorStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<DonorStatus>(request.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var criteria = new DonorSearchCriteria
        {
            Search = request.Search,
            Status = status,
            CompanyTypeId = request.CompanyTypeId,
            RegionCode = request.RegionCode,
            DonationTypeId = request.DonationTypeId,
            DonationFrequencyId = request.DonationFrequencyId,
            RelationshipManagerId = request.RelationshipManagerId,
            // Inclusive of the whole requested day. Kind must be UTC: the column is
            // `timestamptz` and Npgsql rejects an Unspecified-kind DateTime (which is
            // what DateOnly.ToDateTime(TimeOnly) produces by default).
            FollowUpBefore = request.FollowUpBefore?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            SortBy = request.SortBy,
            SortDir = request.SortDir,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var (items, totalCount) = await _donors.SearchAsync(criteria, cancellationToken);

        return PaginatedResult<DonorListItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
