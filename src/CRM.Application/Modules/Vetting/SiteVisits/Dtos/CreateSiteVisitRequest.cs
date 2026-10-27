namespace CRM.Application.Modules.Vetting.SiteVisits.Dtos;

/// <summary>Request body for POST /api/v1/vetting/site-visits. locationLabel defaults to the donor's legal-address suburb.</summary>
public class CreateSiteVisitRequest
{
    public Guid DonorId { get; set; }
    public Guid OfficerId { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public string? LocationLabel { get; set; }
    public string? VisitNotes { get; set; }
}
