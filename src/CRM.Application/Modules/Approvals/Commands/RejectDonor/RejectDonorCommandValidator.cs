namespace CRM.Application.Modules.Approvals.Commands.RejectDonor;

using FluentValidation;

public class RejectDonorCommandValidator : AbstractValidator<RejectDonorCommand>
{
    public RejectDonorCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("rejectionReason is required.")
            .MaximumLength(2_000);
    }
}
