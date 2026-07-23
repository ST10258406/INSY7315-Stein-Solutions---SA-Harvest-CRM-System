namespace CRM.Domain.Enums;

/// <summary>
/// Status of a donor submission moving through the approval pipeline.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Awaiting review by an Admin.</summary>
    Pending,

    /// <summary>Reviewed and approved — donor status becomes Active.</summary>
    Approved,

    /// <summary>Reviewed and rejected — a reason must be recorded.</summary>
    Rejected
}
