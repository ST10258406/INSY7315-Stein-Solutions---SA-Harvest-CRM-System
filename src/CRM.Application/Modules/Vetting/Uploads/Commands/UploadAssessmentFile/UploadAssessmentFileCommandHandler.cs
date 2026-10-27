namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadAssessmentFile;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Commands;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

/// <summary>Sets (or replaces) one of the 5 assessment *_path columns. The old blob is never deleted.</summary>
public class UploadAssessmentFileCommandHandler : IRequestHandler<UploadAssessmentFileCommand, VettingFilesDto>
{
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UploadAssessmentFileCommandHandler(
        IVettingSubmissionRepository submissions, IBlobStorageService blobStorage, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _submissions = submissions;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VettingFilesDto> Handle(UploadAssessmentFileCommand command, CancellationToken cancellationToken)
    {
        if (!VettingUploadSlots.Files.TryGetValue(command.Slot, out var slot))
            throw new NotFoundException($"Unknown file slot '{command.Slot}'.");

        var submission = await _submissions.GetForUpdateAsync(command.SubmissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.SubmissionId);
        SubmissionGuards.EnsureEditable(submission);

        var sniffed = UploadedFileInspector.Sniff(command.FileStream)
            ?? throw new InvalidOperationException("Upload reached the handler without a recognised file signature.");
        var blobPath = $"vetting/{submission.Id}/files/{command.Slot}-{Guid.NewGuid()}{sniffed.Extension}";
        await _blobStorage.UploadAsync(command.FileStream, blobPath, sniffed.MimeType);

        command.OldValues = new { command.Slot, Filled = slot.GetPath(submission.Details!) is not null };

        slot.SetPath(submission.Details!, blobPath);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = submission.Id;
        command.NewValues = new { command.Slot };

        return _mapper.Map<VettingFilesDto>(submission.Details);
    }
}
