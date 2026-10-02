namespace CRM.Infrastructure.Services.Email;

using System.Text.Json.Serialization;

/// <summary>Request/response shapes for POST https://api.brevo.com/v3/smtp/email.</summary>
internal class BrevoSender
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = default!;
}

internal class BrevoReplyTo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = default!;
}

internal class BrevoRecipient
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = default!;
}

internal class BrevoSendEmailRequest
{
    [JsonPropertyName("sender")]
    public BrevoSender Sender { get; set; } = default!;

    [JsonPropertyName("replyTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BrevoReplyTo? ReplyTo { get; set; }

    [JsonPropertyName("to")]
    public List<BrevoRecipient> To { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = default!;

    [JsonPropertyName("htmlContent")]
    public string HtmlContent { get; set; } = default!;
}

internal class BrevoSendEmailResponse
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
}
