namespace CRM.Application.Modules.Vetting.Uploads.Queries.GetUploadDownloadUrl;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// SAS download URL for an evidence photo (<see cref="ShotIndex"/>) or an assessment file
/// (<see cref="Slot"/>); exactly one is set. Audited as Viewed, like donor document downloads.
/// </summary>
public class GetUploadDownloadUrlQuery : IRequest<DocumentDownloadUrlDto>, IAuditableCommand
{
    public Guid SubmissionId { get; set; }
    public int? ShotIndex { get; set; }
    public string? Slot { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => AuditAction.Viewed;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
