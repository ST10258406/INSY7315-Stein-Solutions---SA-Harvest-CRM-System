namespace CRM.Application.Modules.Donors.Commands.SendDonorEmail;

using FluentValidation;

public class SendDonorEmailCommandValidator : AbstractValidator<SendDonorEmailCommand>
{
    public SendDonorEmailCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();

        RuleFor(x => x.To)
            .NotEmpty()
            .Must(to => !to.Contains(',') && !to.Contains(';'))
                .WithMessage("to must be a single recipient — remove any comma- or semicolon-separated addresses.")
            .EmailAddress()
                .WithMessage("to must be a valid email address.");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(10_000);
    }
}
