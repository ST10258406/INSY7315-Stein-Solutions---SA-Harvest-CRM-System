namespace CRM.Application.Modules.Approvals.Queries.GetApprovals;

using CRM.Domain.Enums;
using FluentValidation;

public class GetApprovalsQueryValidator : AbstractValidator<GetApprovalsQuery>
{
    public GetApprovalsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<ApprovalStatus>(s, true, out _))
            .WithMessage($"status must be one of: {string.Join(", ", Enum.GetNames<ApprovalStatus>())}");
    }
}
