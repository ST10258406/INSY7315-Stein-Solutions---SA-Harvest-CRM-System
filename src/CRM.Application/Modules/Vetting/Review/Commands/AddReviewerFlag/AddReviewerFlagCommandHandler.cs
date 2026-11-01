namespace CRM.Application.Modules.Vetting.Review.Commands.AddReviewerFlag;

using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using MediatR;

/// <summary>Adds a reviewer flag while the submission is Ready. Resubmits only recalculate Auto flags, so these stay.</summary>
public class AddReviewerFlagCommandHandler : IRequestHandler<AddReviewerFlagCommand, VettingFlagDto>
{
    private readonly IVettingSubmissionRepository _submissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddReviewerFlagCommandHandler(IVettingSubmissionRepository submissions, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _submissions = submissions;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VettingFlagDto> Handle(AddReviewerFlagCommand command, CancellationToken cancellationToken)
    {
        var submission = await _submissions.GetForUpdateAsync(command.SubmissionId, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), command.SubmissionId);

        if (submission.ApprovalState != VettingApprovalState.Ready)
            throw new ConflictException("Reviewer flags can only be added while the submission is Ready for review.");

        // No Id: EF generates it. A preset key on an entity reached through a tracked parent is treated as an UPDATE.
        var flag = new VettingFlag
        {
            SubmissionId = submission.Id,
            Description = command.Description.Trim(),
            Source = VettingFlagSource.Reviewer
        };
        submission.Flags.Add(flag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = flag.Id;
        command.NewValues = new { flag.SubmissionId, flag.Description, Source = flag.Source.ToString() };

        return _mapper.Map<VettingFlagDto>(flag);
    }
}
