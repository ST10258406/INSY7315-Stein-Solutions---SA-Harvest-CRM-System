namespace CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;

using FluentValidation;

public class GetManagerActivityQueryValidator : AbstractValidator<GetManagerActivityQuery>
{
    public GetManagerActivityQueryValidator()
    {
        RuleFor(x => x.Period)
            .Must(p => string.Equals(p, "weekly", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p, "monthly", StringComparison.OrdinalIgnoreCase))
            .WithMessage("period must be 'weekly' or 'monthly'.");
    }
}
