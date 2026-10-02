namespace CRM.Domain.Entities;

using CRM.Domain.Enums;

/// <summary>
/// Append-only audit record of every outbound email the system has ever attempted
/// to send, separate from <see cref="InteractionLog"/> (donor-interaction history
/// only). Deliberately does not inherit BaseEntity — no UpdatedAt, no soft delete,
/// same pattern as <see cref="AuditLog"/> and <see cref="InteractionLog"/>.
/// </summary>
public class EmailLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    // Nullable: not every email is about a donor (e.g. PasswordReset has none).
    public Guid? DonorId { get; set; }
    public Donor? Donor { get; set; }

    // Null for system-sent emails (e.g. public-form onboarding confirmation).
    public Guid? SentByUserId { get; set; }
    public User? SentByUser { get; set; }

    public EmailType EmailType { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    // The fully rendered content actually sent, never a template reference —
    // templates can change after the fact, but the audit record must reflect
    // exactly what was sent at that point in time.
    public string Body { get; set; } = string.Empty;

    public EmailStatus Status { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
}
