namespace CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

// Implements IAuditableCommand deliberately: AuditBehaviour is registered as an
// open generic IPipelineBehavior<,> that runs for every MediatR request, not
// just commands — it only checks this marker interface. A Query can opt in
// without touching the pipeline, so a "document viewed" audit trail doesn't
// require the handler to write to audit_logs directly (Non-Negotiable rule).
public class GetDocumentDownloadUrlQuery : IRequest<DocumentDownloadUrlDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public Guid DocumentId { get; set; }

    public string EntityType => nameof(DonorDocument);
    public AuditAction Action => AuditAction.Viewed;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
