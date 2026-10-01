namespace CRM.Application.Modules.Users.Commands.UnlockUser;

using FluentValidation;

public class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
