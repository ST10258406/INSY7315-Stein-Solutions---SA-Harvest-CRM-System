namespace CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;

using CRM.Application.Modules.Vetting.Submissions.Validation;
using FluentValidation;

public class CreateSubmissionCommandValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.OfficerId).NotEmpty();
        RuleFor(x => x.SiteVisitId)
            .Must(v => v!.Value != Guid.Empty)
            .When(x => x.SiteVisitId.HasValue)
            .WithMessage("siteVisitId must be a non-empty id.");
        RuleFor(x => x.Details).NotNull().SetValidator(new VettingAssessmentRules()!);
    }
}
