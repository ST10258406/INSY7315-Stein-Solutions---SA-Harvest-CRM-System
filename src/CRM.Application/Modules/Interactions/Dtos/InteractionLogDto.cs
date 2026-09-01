namespace CRM.Application.Modules.Interactions.Dtos;

/// <summary>
/// One row of a donor's append-only interaction timeline. <see cref="EmailAttachmentUrl"/>
/// is a short-lived (15-minute) SAS URL generated at query time — never a raw blob path.
/// </summary>
public class InteractionLogDto
{
    public Guid Id { get; set; }
    public Guid DonorId { get; set; }
    public string InteractionType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? EmailAttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public InteractionUserDto CreatedBy { get; set; } = null!;
}

public class InteractionUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
