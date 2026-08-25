namespace CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DeleteDonorDocumentCommandHandler : IRequestHandler<DeleteDonorDocumentCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteDonorDocumentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteDonorDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.DonorDocuments
            .FirstOrDefaultAsync(
                d => d.Id == request.DocumentId && d.DonorId == request.DonorId,
                cancellationToken);

        if (document is null)
            throw new NotFoundException(nameof(DonorDocument), request.DocumentId);

        // Soft-delete only — never Remove() or a raw DELETE. The blob stays in
        // Azure Storage for audit purposes; this flips a visibility flag only.
        document.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
