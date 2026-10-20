namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class VettingFlag : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public VettingSubmission Submission { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public VettingFlagSource Source { get; set; }

    /// <summary>Old Auto flags are deactivated on resubmit, never deleted.</summary>
    public bool IsActive { get; set; } = true;
}
