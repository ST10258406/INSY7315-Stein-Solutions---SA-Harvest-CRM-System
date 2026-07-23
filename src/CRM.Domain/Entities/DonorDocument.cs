namespace CRM.Domain.Entities;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class DonorDocument : BaseEntity
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobStoragePath { get; set; } = string.Empty;

    public Guid? UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
}
