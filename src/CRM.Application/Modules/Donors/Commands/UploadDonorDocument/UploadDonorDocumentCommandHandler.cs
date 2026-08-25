namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UploadDonorDocumentCommandHandler : IRequestHandler<UploadDonorDocumentCommand, DonorDocumentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UploadDonorDocumentCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blobStorage,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _blobStorage = blobStorage;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<DonorDocumentDto> Handle(UploadDonorDocumentCommand request, CancellationToken cancellationToken)
    {
        var donorExists = await _context.Donors.AnyAsync(d => d.Id == request.DonorId, cancellationToken);
        if (!donorExists)
            throw new NotFoundException(nameof(Donor), request.DonorId);

        var documentType = Enum.Parse<DocumentType>(request.DocumentType);

        // Soft-delete the existing active document of the same type, then upload
        // and insert the new one — both committed together in one SaveChangesAsync
        // call so a failed insert can't leave zero active documents.
        var existingActive = await _context.DonorDocuments
            .Where(d => d.DonorId == request.DonorId && d.DocumentType == documentType && d.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var doc in existingActive)
            doc.IsActive = false;

        // Upload to blob storage before inserting the new row — if the upload
        // fails, we don't want a DB record pointing at a file that doesn't exist.
        // An orphaned blob from a subsequent failed SaveChanges is a cheap, safe
        // failure mode; an orphaned DB reference isn't.
        var blobPath = $"donors/{request.DonorId}/{documentType}/{Guid.NewGuid()}_{request.OriginalFileName}";
        await _blobStorage.UploadAsync(request.FileStream, blobPath, request.ContentType);

        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = request.DonorId,
            DocumentType = documentType,
            FileName = request.OriginalFileName,
            FileSizeBytes = request.FileSizeBytes,
            MimeType = request.ContentType,
            BlobStoragePath = blobPath,
            UploadedByUserId = _currentUserService.GetCurrentUserId(),
            IsActive = true
        };

        _context.DonorDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DonorDocumentDto>(document);
    }
}
