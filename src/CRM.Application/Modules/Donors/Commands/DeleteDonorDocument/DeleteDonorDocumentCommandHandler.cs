namespace CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;

public class DeleteDonorDocumentCommandHandler : IRequestHandler<DeleteDonorDocumentCommand>
{
    private readonly IDonorDocumentRepository _documents;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDonorDocumentCommandHandler(IDonorDocumentRepository documents, IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDonorDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documents.GetForMutationAsync(request.DonorId, request.DocumentId, cancellationToken);

        if (document is null)
            throw new NotFoundException(nameof(DonorDocument), request.DocumentId);

        // Soft-delete only — never Remove() or a raw DELETE. The blob stays in
        // Azure Storage for audit purposes; this flips a visibility flag only.
        document.IsActive = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
