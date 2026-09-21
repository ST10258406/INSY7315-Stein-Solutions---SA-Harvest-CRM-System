namespace CRM.Application.Modules.Users.Commands.SetUserActiveStatus;

using FluentValidation;

public class SetUserActiveStatusCommandValidator : AbstractValidator<SetUserActiveStatusCommand>
{
    public SetUserActiveStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
