using FluentValidation;

namespace CRM.Application.Modules.Users.Queries.GetUsers;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    private static readonly string[] AllowedSortFields = ["name", "email", "createdAt"];

    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.SortDir)
            .Must(d => string.Equals(d, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(d, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("sortDir must be 'asc' or 'desc'");

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortFields)}");
    }
}
