namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;

public class VettingOfficer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AssignedRegion { get; set; } = string.Empty;

    /// <summary>Always recalculated with a COUNT (open visits + open submissions), never ±1.</summary>
    public int WorkloadCount { get; set; }

    public ICollection<VettingSiteVisit> SiteVisits { get; set; } = new List<VettingSiteVisit>();
    public ICollection<VettingSubmission> Submissions { get; set; } = new List<VettingSubmission>();
}
