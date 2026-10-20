namespace CRM.Application.Common.Models;

using CRM.Domain.Enums;

public class VettingSubmissionSearchCriteria
{
    public VettingApprovalState? ApprovalState { get; init; }
    public Guid? DonorId { get; init; }
    public Guid? OfficerId { get; init; }

    /// <summary>Case-insensitive contains on form reference or donor company name.</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
