namespace CRM.Application.Modules.Vetting.Uploads.Queries.GetUploadDownloadUrl;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

/// <summary>Allowed in any approval state. An empty slot is a 404.</summary>
public class GetUploadDownloadUrlQueryHandler : IRequestHandler<GetUploadDownloadUrlQuery, DocumentDownloadUrlDto>
{
    // Same hard ceiling as donor document downloads — never configurable.
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly IVettingSubmissionRepository _submissions;
    private readonly IBlobStorageService _blobStorage;

    public GetUploadDownloadUrlQueryHandler(IVettingSubmissionRepository submissions, IBlobStorageService blobStorage)
    {
        _submissions = submissions;
        _blobStorage = blobStorage;
    }

    public async Task<DocumentDownloadUrlDto> Handle(GetUploadDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        VettingUploadSlots.FileSlot? fileSlot = null;
        if (request.Slot is not null && !VettingUploadSlots.Files.TryGetValue(request.Slot, out fileSlot))
            throw new NotFoundException($"Unknown file slot '{request.Slot}'.");

        var submission = await _submissions.GetForUpdateAsync(request.SubmissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), request.SubmissionId);

        string? path, fileName;
        if (fileSlot is not null)
        {
            path = fileSlot.GetPath(submission.Details!);
            fileName = $"{request.Slot}{Path.GetExtension(path)}";
        }
        else
        {
            var photo = submission.Photos.FirstOrDefault(p => p.ShotIndex == request.ShotIndex);
            path = photo?.BlobStoragePath;
            fileName = photo?.FileName;
        }

        if (path is null)
            throw new NotFoundException("Nothing has been uploaded to this slot yet.");

        request.EntityId = submission.Id;
        request.NewValues = new { Slot = request.Slot ?? $"photo-{request.ShotIndex}" };

        return new DocumentDownloadUrlDto
        {
            DownloadUrl = await _blobStorage.GenerateSasUrlAsync(path, SasExpiry),
            ExpiresAt = DateTime.UtcNow.Add(SasExpiry),
            OriginalFileName = fileName ?? Path.GetFileName(path)
        };
    }
}
