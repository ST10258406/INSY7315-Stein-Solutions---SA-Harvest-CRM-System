namespace CRM.Application.Modules.Donors.Dtos;

/// <summary>Request body for POST /api/v1/donors/{id}/interactions/email.</summary>
public class SendDonorEmailRequest
{
    /// <summary>Single recipient only — no comma/semicolon lists, no CC/BCC.</summary>
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
