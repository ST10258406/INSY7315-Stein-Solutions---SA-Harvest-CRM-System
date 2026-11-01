namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Entities;
using CRM.Domain.Enums;

public class VettingSiteVisit : BaseEntity
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public Guid OfficerId { get; set; }
    public VettingOfficer Officer { get; set; } = null!;

    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public string LocationLabel { get; set; } = string.Empty;
    public string? VisitNotes { get; set; }
    public SiteVisitStatus VisitStatus { get; set; } = SiteVisitStatus.ToDo;
}
