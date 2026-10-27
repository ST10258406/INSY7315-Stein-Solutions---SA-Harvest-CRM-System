namespace CRM.Application.Modules.Agent.Commands.RecordMcpToolCall;

using System.Text.Json;
using MediatR;

/// <summary>Only fills in the audit fields; AuditBehaviour writes the row after this returns.</summary>
public class RecordMcpToolCallCommandHandler : IRequestHandler<RecordMcpToolCallCommand>
{
    public const int MaxArgumentsLength = 4096;

    public Task Handle(RecordMcpToolCallCommand request, CancellationToken cancellationToken)
    {
        var arguments = JsonSerializer.Serialize(request.Arguments ?? new Dictionary<string, JsonElement>());

        request.EntityId = Guid.NewGuid();
        request.NewValues = new
        {
            channel = "mcp",
            tool = request.Tool,
            // Kept as a JSON string so an oversized payload can be cut without breaking the row.
            arguments = arguments.Length > MaxArgumentsLength ? arguments[..MaxArgumentsLength] + "…[truncated]" : arguments,
            isError = request.IsError,
            client = new { name = request.ClientName, version = request.ClientVersion },
        };

        return Task.CompletedTask;
    }
}
