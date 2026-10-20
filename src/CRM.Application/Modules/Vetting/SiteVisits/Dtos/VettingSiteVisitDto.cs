namespace CRM.Application.Modules.Vetting.SiteVisits.Dtos;

public class VettingSiteVisitDto
{
    public Guid Id { get; set; }
    public Guid DonorId { get; set; }
    public string DonorCompanyName { get; set; } = string.Empty;
    public Guid OfficerId { get; set; }
    public string OfficerInitials { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledTime { get; set; }
    public string LocationLabel { get; set; } = string.Empty;
    public string? VisitNotes { get; set; }
    public string VisitStatus { get; set; } = string.Empty;
}
