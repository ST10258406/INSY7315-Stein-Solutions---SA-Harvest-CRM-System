namespace CRM.Application.Modules.Donors.Dtos;

// Input shape only — no Id/Status/SubmissionSource/CreatedAt/UpdatedAt.
// Those are server-set (see CreateDonorCommandHandler); accepting them here
// would let a caller pick their own approval status, which is the exact
// backdoor the Issue 32 acceptance criteria rule out.
public class CreateDonorRequest
{
    public CreateDonorCompanyRequest Company { get; set; } = null!;
    public CreateDonorContactRequest PrimaryContact { get; set; } = null!;
    public CreateDonorContactRequest? MarketingContact { get; set; }
    public CreateDonorContactRequest? AccountsContact { get; set; }
    public CreateDonorLegalAddressRequest LegalAddress { get; set; } = null!;
    public CreateDonorDonationsRequest Donations { get; set; } = null!;
    public CreateDonorComplianceRequest? Compliance { get; set; }
    public CreateDonorCrmRequest? Crm { get; set; }
}

public class CreateDonorCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public short CompanyTypeId { get; set; }
    public string? Website { get; set; }
    public string RegisteredCompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public short EntityTypeId { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? IncomeTaxNumber { get; set; }
}

public class CreateDonorContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class CreateDonorLegalAddressRequest
{
    public string StreetAddress { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public short ProvinceId { get; set; }
    public string PostalCode { get; set; } = string.Empty;
}

public class CreateDonorDonationsRequest
{
    public short FrequencyId { get; set; }
    public List<short> TypeIds { get; set; } = new();
    public string CollectionAddress { get; set; } = string.Empty;
    public string? OperationsLogisticsDetails { get; set; }
    public List<short> RegionIds { get; set; } = new();
}

public class CreateDonorComplianceRequest
{
    public short? BbbeeStatusId { get; set; }
}

public class CreateDonorCrmRequest
{
    public Guid? RelationshipManagerId { get; set; }
    public bool MarketingConsent { get; set; }
    public string? ImpactReportingPreferences { get; set; }
    public string? AdditionalInformation { get; set; }
}
