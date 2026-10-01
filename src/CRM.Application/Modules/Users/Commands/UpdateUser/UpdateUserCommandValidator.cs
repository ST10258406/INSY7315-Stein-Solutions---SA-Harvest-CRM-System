namespace CRM.Application.Modules.Users.Commands.UpdateUser;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Constants;
using FluentValidation;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IUserRepository users)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);

            RuleFor(x => x).MustAsync(async (cmd, ct) =>
                !await users.EmailExistsAsync(cmd.Request.Email, cmd.Id, ct))
                .WithName("Email")
                .WithMessage("A user with this email already exists.")
                .When(x => !string.IsNullOrWhiteSpace(x.Request?.Email));

            RuleFor(x => x.Request.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255)
                // Renaming a staff account into the system-user domain would make it
                // un-manageable (and look like a system actor) — see SystemUsers.
                .Must(email => !SystemUsers.IsSystemUserEmail(email))
                .WithMessage("This email domain is reserved.");
        });
    }
}
