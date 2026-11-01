namespace CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;

using CRM.Application.Modules.Vetting.Submissions.Validation;
using FluentValidation;

public class UpdateSubmissionCommandValidator : AbstractValidator<UpdateSubmissionCommand>
{
    public UpdateSubmissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OfficerId)
            .Must(v => v!.Value != Guid.Empty)
            .When(x => x.OfficerId.HasValue)
            .WithMessage("officerId must be a non-empty id.");
        // PATCH replaces the whole details block, so it is required and fully validated.
        RuleFor(x => x.Details).NotNull().SetValidator(new VettingAssessmentRules()!);
    }
}
