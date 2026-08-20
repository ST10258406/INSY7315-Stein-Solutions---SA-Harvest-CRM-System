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
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsActive { get; set; }
    // Deliberately no BlobStorageUrl / BlobStoragePath property here.
    // If you're tempted to add one "just to have it available", don't —
    // that's exactly the field that leaks into a response by accident.
    // SAS URL generation is Issue 35's job.
}
