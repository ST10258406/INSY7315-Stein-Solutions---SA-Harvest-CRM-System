namespace CRM.Application.Modules.Tasks.Commands.UpdateTask;

using FluentValidation;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().MaximumLength(255)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(10_000)
            .When(x => x.Description is not null);

        RuleFor(x => x.AssignedToUserId)
            .Must(v => v!.Value != Guid.Empty)
            .When(x => x.AssignedToUserId.HasValue)
            .WithMessage("assignedToUserId must be a non-empty id.");

        RuleFor(x => x.DueDate)
            .Must(d => d!.Value >= DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DueDate.HasValue)
            .WithMessage("dueDate must be today or in the future.");
    }
}
