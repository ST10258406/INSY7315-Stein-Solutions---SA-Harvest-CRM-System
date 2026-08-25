namespace CRM.Application.Modules.Donors.Dtos;

public class DonorListItemDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SubmissionSource { get; set; } = string.Empty;
    public RelationshipManagerDto? RelationshipManager { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public DateTime? LastInteractionDate { get; set; }
    public string? LastInteractionType { get; set; }
    public List<string> OperationalRegions { get; set; } = new();
    public string? DonationFrequency { get; set; }
    public List<string> DonationTypes { get; set; } = new();
}

public class RelationshipManagerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
