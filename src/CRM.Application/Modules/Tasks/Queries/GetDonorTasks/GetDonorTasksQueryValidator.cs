namespace CRM.Application.Modules.Tasks.Queries.GetDonorTasks;

using FluentValidation;

public class GetDonorTasksQueryValidator : AbstractValidator<GetDonorTasksQuery>
{
    private static readonly string[] Allowed = ["true", "false", "all"];

    public GetDonorTasksQueryValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.IsCompleted)
            .Must(v => v is not null && Allowed.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("isCompleted must be 'true', 'false' or 'all'.");
    }
}
