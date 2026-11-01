namespace CRM.Application.Modules.Vetting.SiteVisits.Dtos;

/// <summary>Request body for PATCH /api/v1/vetting/site-visits/{id}. A null field is left unchanged.</summary>
public class UpdateSiteVisitRequest
{
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public string? LocationLabel { get; set; }
    public string? VisitNotes { get; set; }
    public string? VisitStatus { get; set; }
    public Guid? OfficerId { get; set; }
}
