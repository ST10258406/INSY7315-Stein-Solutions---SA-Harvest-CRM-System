namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadEvidencePhoto;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UploadEvidencePhotoCommand : IRequest<VettingPhotoDto>, IAuditableCommand
{
    public Guid SubmissionId { get; set; }
    public int ShotIndex { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
