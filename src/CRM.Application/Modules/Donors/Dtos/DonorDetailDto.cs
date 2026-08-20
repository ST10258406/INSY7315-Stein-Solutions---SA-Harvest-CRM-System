namespace CRM.Application.Modules.Donors.Dtos;

public class DonorDetailDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubmissionSource { get; set; } = string.Empty;
    public string? FoodspaceCompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DonorCompanyDto Company { get; set; } = null!;
    public DonorContactDto PrimaryContact { get; set; } = null!;
    public DonorContactDto? MarketingContact { get; set; }
    public DonorContactDto? AccountsContact { get; set; }
    public DonorLegalAddressDto? LegalAddress { get; set; }
    public DonorDonationsDto Donations { get; set; } = null!;
    public DonorComplianceDto Compliance { get; set; } = null!;
    public DonorCrmDto Crm { get; set; } = null!;
}
