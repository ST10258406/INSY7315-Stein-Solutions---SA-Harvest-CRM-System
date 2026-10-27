namespace CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class GetSubmissionsQueryHandler : IRequestHandler<GetSubmissionsQuery, PaginatedResult<VettingSubmissionListItemDto>>
{
    private readonly IVettingSubmissionRepository _submissions;

    public GetSubmissionsQueryHandler(IVettingSubmissionRepository submissions) => _submissions = submissions;

    public async Task<PaginatedResult<VettingSubmissionListItemDto>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _submissions.SearchAsync(new VettingSubmissionSearchCriteria
        {
            ApprovalState = request.ApprovalState is null ? null : Enum.Parse<VettingApprovalState>(request.ApprovalState, ignoreCase: true),
            DonorId = request.DonorId,
            OfficerId = request.OfficerId,
            Search = request.Search,
            SubmittedAtAscending = request.SortBy is null
                ? null
                : string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase),
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return PaginatedResult<VettingSubmissionListItemDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
