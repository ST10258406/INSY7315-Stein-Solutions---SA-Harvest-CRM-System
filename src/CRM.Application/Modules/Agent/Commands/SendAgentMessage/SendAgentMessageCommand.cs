namespace CRM.Application.Modules.Agent.Commands.SendAgentMessage;

using System.Text.Json.Serialization;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// POST /api/v1/agent/messages — one turn of a stateless agent conversation: the client
/// sends the whole conversation so far and gets the assistant's reply back.
/// Audited once per successful request (docs/agent-spec.md §8); the reply text is never stored.
/// </summary>
public record SendAgentMessageCommand : IRequest<AgentReplyDto>, IAuditableCommand
{
    public Guid ConversationId { get; init; }
    public IReadOnlyList<AgentMessageDto> Messages { get; init; } = [];

    public string EntityType => "AgentConversation";
    public AuditAction Action => AuditAction.Viewed;

    // Set by the handler, never bound from the request body — a client must not be able
    // to write its own values into audit_logs.
    [JsonIgnore] public Guid EntityId { get; set; }
    [JsonIgnore] public object? OldValues { get; set; }
    [JsonIgnore] public object? NewValues { get; set; }
}

/// <summary>Role is "user" or "assistant" — clients may never send system or tool messages.</summary>
public record AgentMessageDto(string Role, string Content);

public record AgentReplyDto(Guid ConversationId, string Reply, IReadOnlyList<AgentToolCallDto> ToolCalls, string? Model);

public record AgentToolCallDto(string Name);
