namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class VettingSiteVisit : BaseEntity
{
    public Guid CboId { get; set; }
    public CboOrganisation Cbo { get; set; } = null!;

    public Guid OfficerId { get; set; }
    public VettingOfficer Officer { get; set; } = null!;

    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public string LocationLabel { get; set; } = string.Empty;
    public string? VisitNotes { get; set; }
    public SiteVisitStatus VisitStatus { get; set; } = SiteVisitStatus.ToDo;
}
