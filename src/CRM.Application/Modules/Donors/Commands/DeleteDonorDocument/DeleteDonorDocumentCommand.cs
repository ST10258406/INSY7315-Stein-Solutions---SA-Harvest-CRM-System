namespace CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;

using MediatR;

public class DeleteDonorDocumentCommand : IRequest
{
    public Guid DonorId { get; set; }
    public Guid DocumentId { get; set; }
}
