namespace CRM.Application.Modules.Vetting.Review.Commands.AddReviewerFlag;

using FluentValidation;

public class AddReviewerFlagCommandValidator : AbstractValidator<AddReviewerFlagCommand>
{
    public AddReviewerFlagCommandValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(255);
    }
}
