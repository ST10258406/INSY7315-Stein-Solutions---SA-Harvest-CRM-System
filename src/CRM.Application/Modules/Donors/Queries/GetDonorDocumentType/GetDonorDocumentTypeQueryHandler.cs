namespace CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

// Deliberately separate from GetDocumentDownloadUrlQuery: this is the minimal
// lookup DocumentTypeAuthorizationFilter needs to make its role decision
// *before* the download handler runs. Keeping it its own tiny query means the
// filter never has to duplicate EF/DbContext access itself.
public class GetDonorDocumentTypeQueryHandler : IRequestHandler<GetDonorDocumentTypeQuery, DocumentType>
{
    private readonly IApplicationDbContext _context;

    public GetDonorDocumentTypeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentType> Handle(GetDonorDocumentTypeQuery request, CancellationToken cancellationToken)
    {
        var documentType = await _context.DonorDocuments
            .AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.DonorId == request.DonorId)
            .Select(d => (DocumentType?)d.DocumentType)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentType is null)
            throw new NotFoundException(nameof(DonorDocument), request.DocumentId);

        return documentType.Value;
    }
}
