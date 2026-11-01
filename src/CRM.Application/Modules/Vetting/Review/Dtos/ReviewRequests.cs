namespace CRM.Application.Modules.Vetting.Review.Dtos;

/// <summary>Request body for POST /api/v1/vetting/submissions/{id}/decision.</summary>
public class DecideSubmissionRequest
{
    /// <summary>"Approve", "Return" or "Decline".</summary>
    public string Decision { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>Request body for POST /api/v1/vetting/submissions/{id}/flags.</summary>
public class AddReviewerFlagRequest
{
    public string Description { get; set; } = string.Empty;
}
