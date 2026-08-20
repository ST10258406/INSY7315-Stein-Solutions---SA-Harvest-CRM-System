namespace CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;

using CRM.Application.Modules.Donors.Dtos;
using MediatR;

public class GetDocumentDownloadUrlQuery : IRequest<DocumentDownloadUrlDto>
{
    public Guid DonorId { get; set; }
    public Guid DocumentId { get; set; }
}
