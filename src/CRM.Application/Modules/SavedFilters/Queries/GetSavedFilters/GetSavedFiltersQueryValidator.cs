namespace CRM.Application.Modules.SavedFilters.Queries.GetSavedFilters;

using CRM.Domain.Enums;
using FluentValidation;

public class GetSavedFiltersQueryValidator : AbstractValidator<GetSavedFiltersQuery>
{
    public GetSavedFiltersQueryValidator()
    {
        RuleFor(x => x.Page)
            .Must(p => Enum.TryParse<SavedFilterPage>(p, true, out _))
            .WithMessage($"page must be one of: {string.Join(", ", Enum.GetNames<SavedFilterPage>())}");
    }
}
