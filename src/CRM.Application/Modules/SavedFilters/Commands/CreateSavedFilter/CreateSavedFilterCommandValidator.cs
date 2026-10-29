namespace CRM.Application.Modules.SavedFilters.Commands.CreateSavedFilter;

using CRM.Domain.Enums;
using FluentValidation;

public class CreateSavedFilterCommandValidator : AbstractValidator<CreateSavedFilterCommand>
{
    public const int MaxNameLength = 100;

    public CreateSavedFilterCommandValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n))
            .WithMessage("name is required.")
            .Must(n => (n ?? string.Empty).Trim().Length <= MaxNameLength)
            .WithMessage($"name must be at most {MaxNameLength} characters.");

        RuleFor(x => x.Page)
            .Must(p => Enum.TryParse<SavedFilterPage>(p, true, out _))
            .WithMessage($"page must be one of: {string.Join(", ", Enum.GetNames<SavedFilterPage>())}");

        RuleFor(x => x.QueryString)
            .Cascade(CascadeMode.Stop)
            .Must(q => (q ?? string.Empty).Length <= SavedFilterQueryString.MaxLength)
            .WithMessage($"queryString must be at most {SavedFilterQueryString.MaxLength} characters.")
            .Must(q => SavedFilterQueryString.Normalise(q).Length > 0)
            .WithMessage("queryString must contain at least one filter.")
            .Must(q => !SavedFilterQueryString.IsMalformed(q))
            .WithMessage("queryString is malformed: every key needs a value and may appear only once.")
            .Must(q => SavedFilterQueryString.UnknownKeys(q).Count == 0)
            .WithMessage(x => $"queryString may only contain: {string.Join(", ", SavedFilterQueryString.AllowedKeys)}. " +
                              $"Not allowed: {string.Join(", ", SavedFilterQueryString.UnknownKeys(x.QueryString))}.");
    }
}
