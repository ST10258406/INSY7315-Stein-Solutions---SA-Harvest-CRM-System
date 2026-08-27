namespace CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

// Deliberately separate from GetDocumentDownloadUrlQuery: this is the minimal
// lookup DocumentTypeAuthorizationFilter needs to make its role decision
// *before* the download handler runs. Keeping it its own tiny query means the
// filter never has to reach into persistence itself.
public class GetDonorDocumentTypeQueryHandler : IRequestHandler<GetDonorDocumentTypeQuery, DocumentType>
{
    private readonly IDonorDocumentRepository _documents;

    public GetDonorDocumentTypeQueryHandler(IDonorDocumentRepository documents) => _documents = documents;

    public async Task<DocumentType> Handle(GetDonorDocumentTypeQuery request, CancellationToken cancellationToken)
    {
        var documentType = await _documents.GetDocumentTypeAsync(request.DonorId, request.DocumentId, cancellationToken);

        if (documentType is null)
            throw new NotFoundException(nameof(DonorDocument), request.DocumentId);

        return documentType.Value;
    }
}
