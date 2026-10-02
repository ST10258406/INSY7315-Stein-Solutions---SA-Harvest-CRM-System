using FluentValidation;

namespace CRM.Application.Modules.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        // Server-built, not user input — NotEmpty here is a misconfiguration
        // guard (e.g. Frontend:BaseUrl missing), not a user-facing validation rule.
        RuleFor(x => x.ResetPasswordUrl).NotEmpty();
    }
}
