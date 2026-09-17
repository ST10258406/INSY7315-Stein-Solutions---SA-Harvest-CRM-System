namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using CRM.Application.Common.Interfaces;
using FluentValidation;

public class CreateDonorCommandValidator : AbstractValidator<CreateDonorCommand>
{
    public CreateDonorCommandValidator(ILookupRepository lookups, IUserRepository users)
    {
        RuleFor(x => x.Request).NotNull();

        // All field-level rules live in CreateDonorRequestValidator, shared with
        // SubmitPublicDonorCommandValidator — see that class's remarks.
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request).SetValidator(new CreateDonorRequestValidator(lookups, users));
        });
    }
}
