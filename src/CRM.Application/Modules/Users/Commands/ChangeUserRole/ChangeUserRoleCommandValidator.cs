namespace CRM.Application.Modules.Users.Commands.ChangeUserRole;

using CRM.Application.Common.Interfaces;
using FluentValidation;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator(IUserRepository users)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.RoleId)
                .NotEmpty()
                .MustAsync(async (roleId, ct) => await users.RoleExistsAsync(roleId, ct))
                .WithMessage("Invalid role.");
        });
    }
}
