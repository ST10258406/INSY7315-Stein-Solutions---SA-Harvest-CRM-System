namespace CRM.Application.Modules.Agent.Commands.SendAgentMessage;

using FluentValidation;
using Microsoft.Extensions.Options;

public class SendAgentMessageCommandValidator : AbstractValidator<SendAgentMessageCommand>
{
    public SendAgentMessageCommandValidator(IOptions<AgentOptions> options)
    {
        var agent = options.Value;

        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.Messages)
            .NotNull()
            .Must(m => m.Count >= 1 && m.Count <= agent.MaxHistoryMessages)
            .WithMessage($"messages must contain between 1 and {agent.MaxHistoryMessages} items.");

        RuleFor(x => x.Messages)
            .Must(m => m[^1].Role == "user")
            .When(x => x.Messages is { Count: > 0 })
            .WithMessage("The last message must have role 'user'.");

        RuleForEach(x => x.Messages).ChildRules(message =>
        {
            message.RuleFor(m => m.Role)
                .Must(r => r is "user" or "assistant")
                .WithMessage("role must be 'user' or 'assistant'.");

            message.RuleFor(m => m.Content)
                .NotEmpty()
                .MaximumLength(agent.MaxMessageLength);
        });
    }
}
