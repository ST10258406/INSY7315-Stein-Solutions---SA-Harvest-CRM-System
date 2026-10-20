namespace CRM.Application.Modules.Vetting.Submissions.Dtos;

/// <summary>Request body for POST /api/v1/vetting/submissions.</summary>
public class CreateSubmissionRequest
{
    public Guid DonorId { get; set; }
    public Guid OfficerId { get; set; }
    public Guid? SiteVisitId { get; set; }
    public VettingAssessmentDetailsDto? Details { get; set; }
}

/// <summary>Request body for PATCH /api/v1/vetting/submissions/{id}: replaces details, may reassign the officer.</summary>
public class UpdateSubmissionRequest
{
    public Guid? OfficerId { get; set; }
    public VettingAssessmentDetailsDto? Details { get; set; }
}

public record CreatedSubmissionDto(Guid Id, string FormReference);

public class VettingSubmissionListItemDto
{
    public Guid Id { get; set; }
    public string FormReference { get; set; } = string.Empty;
    public string DonorCompanyName { get; set; } = string.Empty;
    public string OfficerFullName { get; set; } = string.Empty;
    public string ApprovalState { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public int ActiveFlagCount { get; set; }
}

public class VettingSubmissionDto
{
    public Guid Id { get; set; }
    public string FormReference { get; set; } = string.Empty;
    public Guid DonorId { get; set; }
    public string DonorCompanyName { get; set; } = string.Empty;
    public Guid OfficerId { get; set; }
    public string OfficerFullName { get; set; } = string.Empty;
    public Guid? SiteVisitId { get; set; }
    public string ApprovalState { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string? AdminDecision { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public VettingAssessmentDetailsDto Details { get; set; } = null!;
    public List<VettingFlagDto> Flags { get; set; } = [];
    public List<VettingPhotoDto> Photos { get; set; } = [];
    public VettingFilesDto Files { get; set; } = null!;
}

public class VettingFlagDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>An evidence-photo slot. No blob path: downloads go through a SAS endpoint (#158).</summary>
public class VettingPhotoDto
{
    public int ShotIndex { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsCaptured { get; set; }
    public string? FileName { get; set; }
    public DateTime? CapturedAt { get; set; }
}

/// <summary>Which of the 5 assessment file slots are filled. No blob paths.</summary>
public class VettingFilesDto
{
    public bool NpoCertificate { get; set; }
    public bool PboCertificate { get; set; }
    public bool KitchenStoragePhotos { get; set; }
    public bool FacilityPhotos { get; set; }
    public bool Certificates { get; set; }
}
