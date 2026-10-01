namespace CRM.Application.Modules.Users.Commands.CreateUser;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Constants;
using FluentValidation;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IUserRepository users)
    {
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Request.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255)
                .Must(email => !SystemUsers.IsSystemUserEmail(email))
                .WithMessage("This email domain is reserved.")
                .MustAsync(async (email, ct) => !await users.EmailExistsAsync(email, null, ct))
                .WithMessage("A user with this email already exists.");

            RuleFor(x => x.Request.RoleId)
                .NotEmpty()
                .MustAsync(async (roleId, ct) => await users.RoleExistsAsync(roleId, ct))
                .WithMessage("Invalid role.");
        });
    }
}
