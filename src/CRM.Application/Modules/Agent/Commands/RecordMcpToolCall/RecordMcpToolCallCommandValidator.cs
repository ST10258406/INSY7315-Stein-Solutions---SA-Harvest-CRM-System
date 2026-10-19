namespace CRM.Application.Modules.Agent.Commands.RecordMcpToolCall;

using CRM.Application.Modules.Agent.Tools;
using FluentValidation;

public class RecordMcpToolCallCommandValidator : AbstractValidator<RecordMcpToolCallCommand>
{
    public RecordMcpToolCallCommandValidator()
    {
        RuleFor(x => x.Tool)
            .Must(tool => CrmAgentTools.Definitions.Any(d => d.Name == tool))
            .WithMessage("tool must be one of the CRM agent tools.");
    }
}
