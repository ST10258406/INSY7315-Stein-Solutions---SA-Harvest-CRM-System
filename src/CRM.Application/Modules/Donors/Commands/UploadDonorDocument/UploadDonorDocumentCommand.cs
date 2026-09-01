namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using CRM.Application.Modules.Donors.Dtos;
using MediatR;

public class UploadDonorDocumentCommand : IRequest<DonorDocumentDto>
{
    public Guid DonorId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Stream FileStream { get; set; } = null!;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
