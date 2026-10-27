namespace CRM.Domain.Enums;

/// <summary>Review state of a vetting submission: New → Ready → Approved / Returned / Declined.</summary>
public enum VettingApprovalState
{
    New,
    Ready,
    Returned,
    Approved,
    Declined
}
