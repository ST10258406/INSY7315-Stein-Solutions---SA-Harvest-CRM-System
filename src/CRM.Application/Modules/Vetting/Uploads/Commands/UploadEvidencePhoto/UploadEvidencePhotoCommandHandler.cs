namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadEvidencePhoto;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Commands;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

/// <summary>Fills (or replaces) one of the 4 evidence-photo slots. The old blob is never deleted.</summary>
public class UploadEvidencePhotoCommandHandler : IRequestHandler<UploadEvidencePhotoCommand, VettingPhotoDto>
{
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UploadEvidencePhotoCommandHandler(
        IVettingSubmissionRepository submissions, IBlobStorageService blobStorage, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _submissions = submissions;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VettingPhotoDto> Handle(UploadEvidencePhotoCommand command, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetForUpdateAsync(command.SubmissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.SubmissionId);
        SubmissionGuards.EnsureEditable(submission);

        var photo = submission.Photos.FirstOrDefault(p => p.ShotIndex == command.ShotIndex)
            ?? throw new NotFoundException(nameof(VettingEvidencePhoto), command.ShotIndex);

        // Upload before the DB write: an orphaned blob is a cheap failure, a row pointing at nothing isn't.
        // Path and content type come from the sniffed type; the GUID means a re-upload never overwrites an old blob.
        var sniffed = UploadedFileInspector.Sniff(command.FileStream)
            ?? throw new InvalidOperationException("Upload reached the handler without a recognised file signature.");
        var blobPath = $"vetting/{submission.Id}/photos/{photo.ShotIndex}-{Guid.NewGuid()}{sniffed.Extension}";
        await _blobStorage.UploadAsync(command.FileStream, blobPath, sniffed.MimeType);

        command.OldValues = new { Slot = photo.Category.ToString(), photo.FileName };

        photo.IsCaptured = true;
        photo.CapturedAt = DateTime.UtcNow;
        photo.FileName = UploadedFileInspector.SanitizeDisplayName(command.OriginalFileName, VettingUploadSlots.PhotoFileNameMax);
        photo.BlobStoragePath = blobPath;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = submission.Id;
        command.NewValues = new { Slot = photo.Category.ToString(), photo.FileName };

        return _mapper.Map<VettingPhotoDto>(photo);
    }
}
