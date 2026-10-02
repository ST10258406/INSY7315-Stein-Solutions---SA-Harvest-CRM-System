namespace CRM.Infrastructure.Services.Email;

public class BrevoSettings
{
    public string ApiKey { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;
    public string SenderName { get; set; } = default!;

    /// <summary>Shown in the email footer.</summary>
    public string OrganisationName { get; set; } = "SA Harvest";

    /// <summary>Optional extra footer line (address / phone / website). Omitted from the footer when empty.</summary>
    public string? FooterDetails { get; set; }

    /// <summary>
    /// Absolute HTTPS URL of the header logo — email clients can't load relative or embedded images.
    /// Defaults to the frontend's /sa-harvest-logo.png (see AddInfrastructure wiring); when null the
    /// header is text-only.
    /// </summary>
    public string? LogoUrl { get; set; }
}
