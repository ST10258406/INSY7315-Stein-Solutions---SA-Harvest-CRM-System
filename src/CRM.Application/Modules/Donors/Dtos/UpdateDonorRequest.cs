namespace CRM.Application.Modules.Donors.Dtos;

// Partial-update shape: every property nullable. Null section = untouched.
// Present section with a null sub-field = that sub-field untouched.
// Deliberately no Status property — status transitions are a Sprint 4
// (Approvals workflow) concern, not a general field edit (see Issue 33 AC).
public class UpdateDonorRequest
{
    public UpdateDonorCompanyRequest? Company { get; set; }
    public UpdateDonorContactRequest? PrimaryContact { get; set; }
    public UpdateDonorContactRequest? MarketingContact { get; set; }
    public UpdateDonorContactRequest? AccountsContact { get; set; }
    public UpdateDonorLegalAddressRequest? LegalAddress { get; set; }
    public UpdateDonorDonationsRequest? Donations { get; set; }
    public UpdateDonorComplianceRequest? Compliance { get; set; }
    public UpdateDonorCrmRequest? Crm { get; set; }
}

public class UpdateDonorCompanyRequest
{
    public string? CompanyName { get; set; }
    public short? CompanyTypeId { get; set; }
    public string? Website { get; set; }
    public string? RegisteredCompanyName { get; set; }
    public string? TradingName { get; set; }
    public short? EntityTypeId { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? IncomeTaxNumber { get; set; }
}

public class UpdateDonorContactRequest
{
    public string? Name { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateDonorLegalAddressRequest
{
    public string? StreetAddress { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public short? ProvinceId { get; set; }
    public string? PostalCode { get; set; }
}

public class UpdateDonorDonationsRequest
{
    public short? FrequencyId { get; set; }
    public List<short>? TypeIds { get; set; }
    public string? CollectionAddress { get; set; }
    public string? OperationsLogisticsDetails { get; set; }
    public List<short>? RegionIds { get; set; }
}

public class UpdateDonorComplianceRequest
{
    public short? BbbeeStatusId { get; set; }
}

public class UpdateDonorCrmRequest
{
    public Guid? RelationshipManagerId { get; set; }
    public bool? MarketingConsent { get; set; }
    public string? ImpactReportingPreferences { get; set; }
    public string? AdditionalInformation { get; set; }
}
