namespace CRM.Application.Modules.Interactions.Commands.LogInteraction;

using CRM.Domain.Enums;
using FluentValidation;

public class LogInteractionCommandValidator : AbstractValidator<LogInteractionCommand>
{
    public LogInteractionCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();

        RuleFor(x => x.InteractionType)
            .Must(t => Enum.TryParse<InteractionType>(t, true, out _))
            .WithMessage($"interactionType must be one of: {string.Join(", ", Enum.GetNames<InteractionType>())}");

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(10_000);

        RuleFor(x => x.Subject)
            .MaximumLength(255);

        RuleFor(x => x.FollowUpDate)
            .Must(d => d!.Value > DateTime.UtcNow)
            .When(x => x.FollowUpDate.HasValue)
            .WithMessage("followUpDate must be in the future.");
    }
}
