namespace CRM.Domain.Entities;

using CRM.Domain.Enums;

public class InteractionLog
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public InteractionType InteractionType { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? EmailAttachmentUrl { get; set; }
}
