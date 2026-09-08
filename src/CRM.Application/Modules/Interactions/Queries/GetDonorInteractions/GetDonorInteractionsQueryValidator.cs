namespace CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;

using CRM.Domain.Enums;
using FluentValidation;

public class GetDonorInteractionsQueryValidator : AbstractValidator<GetDonorInteractionsQuery>
{
    public GetDonorInteractionsQueryValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.InteractionType)
            .Must(t => Enum.TryParse<InteractionType>(t, true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.InteractionType))
            .WithMessage($"interactionType must be one of: {string.Join(", ", Enum.GetNames<InteractionType>())}");
    }
}
