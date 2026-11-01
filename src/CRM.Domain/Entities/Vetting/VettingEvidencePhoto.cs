namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class VettingEvidencePhoto : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public VettingSubmission Submission { get; set; } = null!;

    /// <summary>Slot 0–3, one per <see cref="EvidencePhotoCategory"/>.</summary>
    public int ShotIndex { get; set; }
    public EvidencePhotoCategory Category { get; set; }
    public bool IsCaptured { get; set; }

    /// <summary>Display name only; the blob is at <see cref="BlobStoragePath"/>.</summary>
    public string? FileName { get; set; }
    public string? BlobStoragePath { get; set; }
    public DateTime? CapturedAt { get; set; }
}
