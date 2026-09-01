namespace CRM.Application.Modules.Approvals.Queries.GetApprovals;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Approvals.Dtos;
using MediatR;

/// <summary>GET /api/v1/approvals — the donor approval queue. Defaults to Pending, newest first.</summary>
public record GetApprovalsQuery : PaginationParams, IRequest<PaginatedResult<ApprovalDto>>
{
    public string Status { get; init; } = "Pending";
}
