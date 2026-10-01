namespace CRM.Application.Modules.PublicDonors.Dtos;

using CRM.Application.Modules.Donors.Dtos;

/// <summary>
/// Body of POST /public/donors/submit. A dedicated type rather than a subclass of
/// CreateDonorRequest: it contains only what an anonymous donor may supply, so
/// staff-only CRM fields (RelationshipManagerId, AdditionalInformation) do not exist
/// here and cannot be mass-assigned, even if present in the JSON body.
/// The nested section types are shared with CreateDonorRequest so the JSON shape
/// stays identical to the internal endpoint's.
/// </summary>
public class SubmitPublicDonorRequest
{
    public CreateDonorCompanyRequest Company { get; set; } = null!;
    public CreateDonorContactRequest PrimaryContact { get; set; } = null!;
    public CreateDonorContactRequest? MarketingContact { get; set; }
    public CreateDonorContactRequest? AccountsContact { get; set; }
    public CreateDonorLegalAddressRequest LegalAddress { get; set; } = null!;
    public CreateDonorDonationsRequest Donations { get; set; } = null!;
    public CreateDonorComplianceRequest? Compliance { get; set; }
    public PublicDonorPreferencesRequest? Crm { get; set; }
    public PublicDonorSignatureRequest Signature { get; set; } = null!;

    /// <summary>
    /// Projects onto CreateDonorRequest so the public endpoint validates through the
    /// exact same rules as POST /donors. Staff-only fields are left unset.
    /// </summary>
    public CreateDonorRequest ToCreateDonorRequest() => new()
    {
        Company = Company,
        PrimaryContact = PrimaryContact,
        MarketingContact = MarketingContact,
        AccountsContact = AccountsContact,
        LegalAddress = LegalAddress,
        Donations = Donations,
        Compliance = Compliance,
        Crm = Crm is null ? null : new CreateDonorCrmRequest
        {
            MarketingConsent = Crm.MarketingConsent,
            ImpactReportingPreferences = Crm.ImpactReportingPreferences
        }
    };
}

/// <summary>The only "crm" fields a donor may supply about themselves.</summary>
public class PublicDonorPreferencesRequest
{
    public bool MarketingConsent { get; set; }
    public string? ImpactReportingPreferences { get; set; }
}

public class PublicDonorSignatureRequest
{
    /// <summary>A "data:image/png;base64,..." data URI — see Base64PngDecoder.</summary>
    public string ImageBase64 { get; set; } = string.Empty;
}
