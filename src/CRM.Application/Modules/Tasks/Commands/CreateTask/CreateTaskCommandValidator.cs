namespace CRM.Application.Modules.Tasks.Commands.CreateTask;

using FluentValidation;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(10_000);

        RuleFor(x => x.AssignedToUserId).NotEmpty();

        RuleFor(x => x.DueDate)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("dueDate must be today or in the future.");
    }
}
