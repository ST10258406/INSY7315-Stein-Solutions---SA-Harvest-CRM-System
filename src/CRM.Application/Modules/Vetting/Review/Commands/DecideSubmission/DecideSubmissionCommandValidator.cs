namespace CRM.Application.Modules.Vetting.Review.Commands.DecideSubmission;

using CRM.Domain.Enums;
using FluentValidation;

public class DecideSubmissionCommandValidator : AbstractValidator<DecideSubmissionCommand>
{
    public DecideSubmissionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        // Names only: Enum.TryParse would also accept "1".
        RuleFor(x => x.Decision)
            .Must(d => Enum.GetNames<VettingDecision>().Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("decision must be one of: Approve, Return, Decline.");

        // Same rules as RejectDonorCommandValidator; NotEmpty also rejects whitespace-only.
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("reason is required to return or decline a submission.")
            .When(x => !string.Equals(x.Decision, nameof(VettingDecision.Approve), StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Reason).MaximumLength(2_000);
    }
}
