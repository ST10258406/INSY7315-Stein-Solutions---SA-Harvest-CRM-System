namespace CRM.Domain.Enums;

/// <summary>
/// Lifecycle status of a donor record.
/// </summary>
public enum DonorStatus
{
    /// <summary>Newly submitted or captured, awaiting Admin review.</summary>
    PendingReview,

    /// <summary>Approved and currently an active donor relationship.</summary>
    Active,

    /// <summary>Previously active but has gone quiet — no recent donations/interactions.</summary>
    Lapsed,

    /// <summary>Submission was reviewed and rejected by an Admin.</summary>
    Rejected
}
