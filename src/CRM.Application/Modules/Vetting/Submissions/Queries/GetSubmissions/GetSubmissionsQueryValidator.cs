namespace CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;

using CRM.Domain.Enums;
using FluentValidation;

public class GetSubmissionsQueryValidator : AbstractValidator<GetSubmissionsQuery>
{
    public GetSubmissionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.ApprovalState)
            .Must(s => Enum.TryParse<VettingApprovalState>(s, true, out _))
            .When(x => x.ApprovalState is not null)
            .WithMessage("approvalState must be one of: New, Ready, Returned, Approved, Declined.");
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy)
            .Must(s => string.Equals(s, "submittedAt", StringComparison.OrdinalIgnoreCase))
            .When(x => x.SortBy is not null)
            .WithMessage("sortBy may only be submittedAt.");
        RuleFor(x => x.SortDir)
            .Must(s => s.Equals("asc", StringComparison.OrdinalIgnoreCase) || s.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("sortDir must be asc or desc.");
    }
}
