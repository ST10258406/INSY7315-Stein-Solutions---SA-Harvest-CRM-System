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
    }
}
