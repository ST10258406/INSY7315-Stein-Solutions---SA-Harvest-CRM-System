namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

public class UploadDonorDocumentCommandHandler : IRequestHandler<UploadDonorDocumentCommand, DonorDocumentDto>
{
    private readonly IDonorRepository _donors;
    private readonly IDonorDocumentRepository _documents;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UploadDonorDocumentCommandHandler(
        IDonorRepository donors,
        IDonorDocumentRepository documents,
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorage,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _donors = donors;
        _documents = documents;
        _unitOfWork = unitOfWork;
        _blobStorage = blobStorage;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<DonorDocumentDto> Handle(UploadDonorDocumentCommand request, CancellationToken cancellationToken)
    {
        var donorExists = await _donors.ExistsAsync(request.DonorId, cancellationToken);
        if (!donorExists)
            throw new NotFoundException(nameof(Donor), request.DonorId);

        var documentType = Enum.Parse<DocumentType>(request.DocumentType);

        // Soft-delete the existing active document of the same type, then upload
        // and insert the new one — both committed together in one SaveChangesAsync
        // call so a failed insert can't leave zero active documents.
        var existingActive = await _documents.GetActiveByDonorAndTypeAsync(
            request.DonorId, documentType, cancellationToken);

        foreach (var doc in existingActive)
            doc.IsActive = false;

        // Upload to blob storage before inserting the new row — if the upload
        // fails, we don't want a DB record pointing at a file that doesn't exist.
        // An orphaned blob from a subsequent failed SaveChanges is a cheap, safe
        // failure mode; an orphaned DB reference isn't.
        // The blob name and content type come from the sniffed file type, never from
        // client-supplied values; the client filename is kept only as a sanitised display name.
        var sniffed = UploadedFileInspector.Sniff(request.FileStream)
            ?? throw new InvalidOperationException("Upload reached the handler without a recognised file signature.");
        var blobPath = $"donors/{request.DonorId}/{documentType}/{Guid.NewGuid()}{sniffed.Extension}";
        await _blobStorage.UploadAsync(request.FileStream, blobPath, sniffed.MimeType);

        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = request.DonorId,
            DocumentType = documentType,
            FileName = UploadedFileInspector.SanitizeDisplayName(request.OriginalFileName),
            FileSizeBytes = UploadedFileInspector.RealLength(request.FileStream, request.FileSizeBytes),
            MimeType = sniffed.MimeType,
            BlobStoragePath = blobPath,
            UploadedByUserId = _currentUserService.GetCurrentUserId(),
            IsActive = true
        };

        await _documents.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DonorDocumentDto>(document);
    }
}
