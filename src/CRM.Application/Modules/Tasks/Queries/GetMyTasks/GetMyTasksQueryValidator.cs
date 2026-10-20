namespace CRM.Application.Modules.Tasks.Queries.GetMyTasks;

using FluentValidation;

public class GetMyTasksQueryValidator : AbstractValidator<GetMyTasksQuery>
{
    public const int MaxRangeDays = 62;

    public GetMyTasksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        // The calendar (Issue 104) fetches one month at a time; cap the window so a range can't
        // be used to pull every task ever.
        RuleFor(x => x)
            .Must(x => x.DueAfter!.Value <= x.DueBefore!.Value)
            .When(x => x.DueAfter.HasValue && x.DueBefore.HasValue)
            .WithName("dueAfter")
            .WithMessage("dueAfter must be on or before dueBefore.");
        RuleFor(x => x)
            .Must(x => x.DueBefore!.Value.DayNumber - x.DueAfter!.Value.DayNumber <= MaxRangeDays)
            .When(x => x.DueAfter.HasValue && x.DueBefore.HasValue)
            .WithName("dueAfter")
            .WithMessage($"The dueAfter–dueBefore range can be at most {MaxRangeDays} days.");
    }
}
