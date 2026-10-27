using System.Text.Json;
using CRM.Application.Modules.Agent.Commands.RecordMcpToolCall;
using CRM.Application.Modules.Agent.Tools;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Agent;

/// <summary>docs/mcp-spec.md §6 (audit) and §7 (one tool definition for both channels).</summary>
public class RecordMcpToolCallCommandTests
{
    private readonly RecordMcpToolCallCommandHandler _handler = new();
    private readonly RecordMcpToolCallCommandValidator _validator = new();

    private static Dictionary<string, JsonElement> Args(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Fact]
    public async Task Handle_SetsAuditFields_WithoutResult()
    {
        var command = new RecordMcpToolCallCommand
        {
            Tool = "search_donors",
            Arguments = Args("""{"search":"Woolworths"}"""),
            IsError = true,
            ClientName = "claude-code",
            ClientVersion = "2.1.0",
        };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("McpToolCall", command.EntityType);
        Assert.Equal(AuditAction.Viewed, command.Action);
        Assert.NotEqual(Guid.Empty, command.EntityId);
        Assert.Null(command.OldValues);
        var json = JsonSerializer.Serialize(command.NewValues);
        Assert.Contains("\"channel\":\"mcp\"", json);
        Assert.Contains("\"tool\":\"search_donors\"", json);
        Assert.Contains("Woolworths", json);
        Assert.Contains("\"isError\":true", json);
        Assert.Contains("claude-code", json);
    }

    [Fact]
    public async Task Handle_TruncatesOversizedArguments()
    {
        var command = new RecordMcpToolCallCommand
        {
            Tool = "search_donors",
            Arguments = Args($$"""{"search":"{{new string('x', 10_000)}}"}"""),
        };

        await _handler.Handle(command, CancellationToken.None);

        var arguments = (string)command.NewValues!.GetType().GetProperty("arguments")!.GetValue(command.NewValues)!;
        Assert.True(arguments.Length <= RecordMcpToolCallCommandHandler.MaxArgumentsLength + "…[truncated]".Length);
        Assert.EndsWith("…[truncated]", arguments);
    }

    [Fact]
    public async Task Handle_NewEntityIdPerCall()
    {
        var first = new RecordMcpToolCallCommand { Tool = "run_report" };
        var second = new RecordMcpToolCallCommand { Tool = "run_report" };

        await _handler.Handle(first, CancellationToken.None);
        await _handler.Handle(second, CancellationToken.None);

        Assert.NotEqual(first.EntityId, second.EntityId);
    }

    [Fact]
    public void Validator_RejectsEmptyOrOversizedToolName()
    {
        Assert.False(_validator.Validate(new RecordMcpToolCallCommand { Tool = "" }).IsValid);
        Assert.False(_validator.Validate(new RecordMcpToolCallCommand { Tool = new string('x', 101) }).IsValid);
    }

    // A call to a tool that doesn't exist is still an attempt worth an audit row.
    [Fact]
    public void Validator_AcceptsUnknownToolName() =>
        Assert.True(_validator.Validate(new RecordMcpToolCallCommand { Tool = "delete_donor" }).IsValid);

    [Fact]
    public void Validator_AcceptsEveryDefinedTool() =>
        Assert.All(CrmAgentTools.Definitions, d =>
            Assert.True(_validator.Validate(new RecordMcpToolCallCommand { Tool = d.Name }).IsValid));

    [Fact]
    public void Definitions_HasTenUniqueNamesWithDescriptions()
    {
        Assert.Equal(10, CrmAgentTools.Definitions.Count);
        Assert.Equal(10, CrmAgentTools.Definitions.Select(d => d.Name).Distinct().Count());
        Assert.All(CrmAgentTools.Definitions, d =>
            Assert.NotNull(d.Method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).SingleOrDefault()));
    }

    [Fact]
    public void AsTools_MatchesDefinitions() =>
        Assert.Equal(
            CrmAgentTools.Definitions.Select(d => d.Name),
            new CrmAgentTools(Substitute.For<ISender>()).AsTools().Cast<AIFunction>().Select(t => t.Name));
}
