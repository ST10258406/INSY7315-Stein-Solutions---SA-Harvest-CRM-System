using CRM.Domain.Enums;
using FluentValidation;

namespace CRM.Application.Modules.Donors.Queries.GetDonors;

public class GetDonorsQueryValidator : AbstractValidator<GetDonorsQuery>
{
    public const int MaxRangeDays = 62;

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

        // Same window rule as GetMyTasksQuery (calendar, Issue 104).
        RuleFor(x => x)
            .Must(x => x.FollowUpAfter!.Value <= x.FollowUpBefore!.Value)
            .When(x => x.FollowUpAfter.HasValue && x.FollowUpBefore.HasValue)
            .WithName("followUpAfter")
            .WithMessage("followUpAfter must be on or before followUpBefore.");
        RuleFor(x => x)
            .Must(x => x.FollowUpBefore!.Value.DayNumber - x.FollowUpAfter!.Value.DayNumber <= MaxRangeDays)
            .When(x => x.FollowUpAfter.HasValue && x.FollowUpBefore.HasValue)
            .WithName("followUpAfter")
            .WithMessage($"The followUpAfter–followUpBefore range can be at most {MaxRangeDays} days.");
    }
}
