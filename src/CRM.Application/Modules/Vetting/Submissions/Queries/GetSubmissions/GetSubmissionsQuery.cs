namespace CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using MediatR;

public record GetSubmissionsQuery : PaginationParams, IRequest<PaginatedResult<VettingSubmissionListItemDto>>
{
    public string? ApprovalState { get; init; }
    public Guid? DonorId { get; init; }
    public Guid? OfficerId { get; init; }
    public string? Search { get; init; }
}
