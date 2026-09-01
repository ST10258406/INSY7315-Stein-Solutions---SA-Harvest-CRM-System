namespace CRM.Application.Modules.Tasks.Queries.GetMyTasks;

using FluentValidation;

public class GetMyTasksQueryValidator : AbstractValidator<GetMyTasksQuery>
{
    public GetMyTasksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
