namespace CRM.Application.Modules.Tasks.Queries.GetMyTasks;

using CRM.Application.Common.Validation;
using FluentValidation;

public class GetMyTasksQueryValidator : AbstractValidator<GetMyTasksQuery>
{
    public GetMyTasksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x).DateWindow(x => x.DueAfter, x => x.DueBefore, "dueAfter", "dueBefore");
    }
}
