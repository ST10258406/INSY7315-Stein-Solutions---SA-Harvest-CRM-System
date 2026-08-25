using CRM.Domain.Enums;
using FluentValidation;

namespace CRM.Application.Modules.Donors.Queries.GetDonors;

public class GetDonorsQueryValidator : AbstractValidator<GetDonorsQuery>
{
    private static readonly string[] AllowedSortFields = ["followUpDate", "companyName", "createdAt", "lastInteractionDate"];

    public GetDonorsQueryValidator()
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

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<DonorStatus>(s, true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("status must be one of: PendingReview, Active, Lapsed, Rejected");
    }
}
