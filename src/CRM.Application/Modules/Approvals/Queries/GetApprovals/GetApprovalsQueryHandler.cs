namespace CRM.Application.Modules.Approvals.Queries.GetApprovals;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class GetApprovalsQueryHandler : IRequestHandler<GetApprovalsQuery, PaginatedResult<ApprovalDto>>
{
    private readonly IApprovalRepository _approvals;

    public GetApprovalsQueryHandler(IApprovalRepository approvals) => _approvals = approvals;

    public async Task<PaginatedResult<ApprovalDto>> Handle(GetApprovalsQuery request, CancellationToken cancellationToken)
    {
        // Validator has already guaranteed this parses.
        var status = Enum.Parse<ApprovalStatus>(request.Status, ignoreCase: true);

        var (items, totalCount) = await _approvals.GetApprovalsAsync(
            status, request.Page, request.PageSize, cancellationToken);

        return PaginatedResult<ApprovalDto>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
