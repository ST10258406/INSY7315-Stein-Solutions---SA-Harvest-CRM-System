namespace CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;

using CRM.Domain.Enums;
using MediatR;

public class GetDonorDocumentTypeQuery : IRequest<DocumentType>
{
    public Guid DonorId { get; set; }
    public Guid DocumentId { get; set; }
}
