namespace CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand, VettingSubmissionDto>
{
    private readonly IVettingOfficerRepository _officers;
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateSubmissionCommandHandler(
        IVettingOfficerRepository officers, IVettingSubmissionRepository submissions, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _officers = officers;
        _submissions = submissions;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VettingSubmissionDto> Handle(UpdateSubmissionCommand command, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.Id);

        SubmissionGuards.EnsureEditable(submission);

        var previousOfficerId = submission.OfficerId;
        if (command.OfficerId is { } newOfficer
            && newOfficer != previousOfficerId
            && !await _officers.ExistsAsync(newOfficer, cancellationToken))
        {
            throw new NotFoundException(nameof(VettingOfficer), newOfficer);
        }

        command.OldValues = new { submission.OfficerId, Details = _mapper.Map<VettingAssessmentDetailsDto>(submission.Details) };

        // Maps onto the tracked row, so its id, submission link and upload paths are kept.
        _mapper.Map(command.Details, submission.Details);
        if (command.OfficerId is { } officerId) submission.OfficerId = officerId;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (submission.OfficerId != previousOfficerId)
            {
                await _officers.RecalculateWorkloadAsync(previousOfficerId, cancellationToken);
                await _officers.RecalculateWorkloadAsync(submission.OfficerId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return true;
        }, cancellationToken);

        command.EntityId = submission.Id;
        command.NewValues = new { submission.OfficerId, command.Details };

        return await _submissions.GetDtoByIdAsync(submission.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"VettingSubmission {submission.Id} could not be re-read immediately after being updated.");
    }
}
