namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadAssessmentFile;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UploadAssessmentFileCommand : IRequest<VettingFilesDto>, IAuditableCommand
{
    public Guid SubmissionId { get; set; }

    /// <summary>A key of <see cref="VettingUploadSlots.Files"/>, e.g. "npo-certificate".</summary>
    public string Slot { get; set; } = string.Empty;
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
