namespace CRM.Application.Modules.Agent.Commands.RecordMcpToolCall;

using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Sent by the MCP call-tool filter (CRM.API McpExtensions) after every /mcp tool call, so
/// AuditBehaviour writes one audit_logs row per call (docs/mcp-spec.md §6). The tool's
/// result is never passed in: it is derived data and would duplicate PII into audit_logs.
/// </summary>
public record RecordMcpToolCallCommand : IRequest, IAuditableCommand
{
    public string Tool { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, JsonElement>? Arguments { get; init; }
    public bool IsError { get; init; }
    public string? ClientName { get; init; }
    public string? ClientVersion { get; init; }

    public string EntityType => "McpToolCall";
    public AuditAction Action => AuditAction.Viewed;

    // Set by the handler.
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
