namespace CRM.Application.Modules.Donors.Dtos;

public class DonorCrmDto
{
    public RelationshipManagerDto? RelationshipManager { get; set; }
    public bool MarketingConsent { get; set; }
    public DateTime? MarketingConsentDate { get; set; }
    public string? ImpactReportingPreferences { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? AdditionalInformation { get; set; }
}
