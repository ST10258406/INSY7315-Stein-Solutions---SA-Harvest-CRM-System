namespace CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;

using FluentValidation;

public class SendPublicFormInviteCommandValidator : AbstractValidator<SendPublicFormInviteCommand>
{
    public SendPublicFormInviteCommandValidator()
    {
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

        // Server-built, not user input — NotEmpty here is a misconfiguration
        // guard (e.g. Frontend:BaseUrl missing), not a user-facing validation rule.
        RuleFor(x => x.PublicFormUrl).NotEmpty();
    }
}
