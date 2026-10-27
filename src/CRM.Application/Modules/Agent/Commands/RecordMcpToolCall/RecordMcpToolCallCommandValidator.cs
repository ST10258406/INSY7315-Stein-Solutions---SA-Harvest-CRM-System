namespace CRM.Application.Modules.Agent.Commands.RecordMcpToolCall;

using FluentValidation;

public class RecordMcpToolCallCommandValidator : AbstractValidator<RecordMcpToolCallCommand>
{
    public const int MaxToolNameLength = 100;

    public RecordMcpToolCallCommandValidator()
    {
        // Not limited to CrmAgentTools.Definitions: calls to unknown tools are audited too.
        RuleFor(x => x.Tool)
            .NotEmpty()
            .MaximumLength(MaxToolNameLength);
    }
}
