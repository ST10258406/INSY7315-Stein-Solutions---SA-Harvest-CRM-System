namespace CRM.Infrastructure.Services.Email;

public class BrevoSettings
{
    public string ApiKey { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;
    public string SenderName { get; set; } = default!;
}
