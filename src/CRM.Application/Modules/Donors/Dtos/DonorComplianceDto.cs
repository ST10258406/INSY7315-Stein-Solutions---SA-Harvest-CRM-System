using CRM.Application.Common.Models;

namespace CRM.Application.Modules.Donors.Dtos;

public class DonorComplianceDto
{
    public LookupDto? BbbeeStatus { get; set; }
    public List<DonorDocumentDto> Documents { get; set; } = new();
}

public class DonorDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    // Deliberately no BlobStorageUrl / BlobStoragePath property here.
    // If you're tempted to add one "just to have it available", don't —
    // that's exactly the field that leaks into a response by accident.
    // FileSizeBytes/MimeType/IsActive from the SDD shape aren't stored on
    // DonorDocument yet (see DonorDocumentConfiguration) — omitted rather
    // than faked; flagged as a follow-up schema gap, not guessed at here.
}
