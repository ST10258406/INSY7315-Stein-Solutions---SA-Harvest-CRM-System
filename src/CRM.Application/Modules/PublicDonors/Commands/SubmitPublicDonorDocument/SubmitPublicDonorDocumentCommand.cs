namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

using CRM.Application.Modules.PublicDonors.Dtos;
using MediatR;

// Deliberately does NOT implement IAuditableCommand — same reasoning as
// SubmitPublicDonorCommand: there is no authenticated caller for AuditBehaviour
// to attribute this to. The handler writes its own audit_logs row instead.
public class SubmitPublicDonorDocumentCommand : IRequest<SubmitPublicDonorDocumentResponseDto>
{
    /// <summary>
    /// One-time opaque credential returned by POST /public/donors/submit
    /// (Donor.SubmissionToken) — resolves server-side to the pending donor. The
    /// public client never sees or sends the donor's real Id.
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>Raw string from the multipart form — only "BBBEECertificate" is accepted.</summary>
    public string DocumentType { get; set; } = string.Empty;

    public Stream FileStream { get; set; } = Stream.Null;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>Captured for the manual audit_logs row — see remarks above.</summary>
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
