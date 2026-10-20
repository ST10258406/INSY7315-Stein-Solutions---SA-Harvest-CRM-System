namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Enums;

/// <summary>Parent audit record. Details, flags and photos hang off it.</summary>
public class VettingSubmission : BaseEntity
{
    /// <summary>VET-{year}-{4-digit sequence}, assigned server-side.</summary>
    public string FormReference { get; set; } = string.Empty;

    public Guid CboId { get; set; }
    public CboOrganisation Cbo { get; set; } = null!;

    public Guid OfficerId { get; set; }
    public VettingOfficer Officer { get; set; } = null!;

    public Guid? SiteVisitId { get; set; }
    public VettingSiteVisit? SiteVisit { get; set; }

    public VettingApprovalState ApprovalState { get; set; } = VettingApprovalState.New;
    public DateTime? SubmittedAt { get; set; }
    public VettingDecision? AdminDecision { get; set; }
    public string? DecisionReason { get; set; }

    public VettingAssessmentDetails? Details { get; set; }
    public ICollection<VettingFlag> Flags { get; set; } = new List<VettingFlag>();
    public ICollection<VettingEvidencePhoto> Photos { get; set; } = new List<VettingEvidencePhoto>();
}
