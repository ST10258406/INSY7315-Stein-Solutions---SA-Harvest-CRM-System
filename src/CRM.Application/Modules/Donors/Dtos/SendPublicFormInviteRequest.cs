namespace CRM.Application.Modules.Donors.Dtos;

/// <summary>Request body for POST /api/v1/donors/public-form-invite.</summary>
public class SendPublicFormInviteRequest
{
    /// <summary>Single recipient only — no comma/semicolon lists, no CC/BCC.</summary>
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
