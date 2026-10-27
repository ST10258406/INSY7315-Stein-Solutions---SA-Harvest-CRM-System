using System.Text.Json;
using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Agent;
using CRM.Application.Modules.Agent.Commands.SendAgentMessage;
using CRM.Application.Modules.Agent.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Agent;

public class SendAgentMessageCommandHandlerTests
{
    private readonly IChatClient _chatClient = Substitute.For<IChatClient>();
    private readonly SendAgentMessageCommandHandler _handler;

    private List<ChatMessage> _sentMessages = [];
    private ChatOptions? _sentOptions;

    public SendAgentMessageCommandHandlerTests()
    {
        _handler = new SendAgentMessageCommandHandler(
            _chatClient,
            new CrmAgentTools(Substitute.For<ISender>()),
            Options.Create(AgentSettings()));

        // Stands in for FunctionInvokingChatClient: the response carries the tool round it ran.
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _sentMessages = call.Arg<IEnumerable<ChatMessage>>().ToList();
                _sentOptions = call.Arg<ChatOptions?>();
                return new ChatResponse(
                [
                    new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "search_donors", new Dictionary<string, object?> { ["status"] = "Active" }), new FunctionCallContent("c2", "get_dashboard_stats")]),
                    new ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "{}"), new FunctionResultContent("c2", "{}")]),
                    new ChatMessage(ChatRole.Assistant, "There are 4 pending donors."),
                ]) { ModelId = "qwen2.5:7b" };
            });
    }

    private static AgentOptions AgentSettings(bool enabled = true, int timeoutSeconds = 60) =>
        new() { Enabled = enabled, Provider = "openai-compatible", Model = "configured-model", TimeoutSeconds = timeoutSeconds };

    private SendAgentMessageCommandHandler HandlerWith(AgentOptions options) =>
        new(_chatClient, new CrmAgentTools(Substitute.For<ISender>()), Options.Create(options));

    private static SendAgentMessageCommand Command() => new()
    {
        ConversationId = Guid.NewGuid(),
        Messages = [new("user", "How many pending?"), new("assistant", "Let me check."), new("user", "And active?")],
    };

    [Fact]
    public async Task Handle_SendsSystemPromptThenConversation()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal(ChatRole.System, _sentMessages[0].Role);
        Assert.Contains("SA Harvest CRM assistant", _sentMessages[0].Text);
        Assert.Equal([ChatRole.User, ChatRole.Assistant, ChatRole.User], _sentMessages.Skip(1).Select(m => m.Role));
        Assert.Equal("And active?", _sentMessages[^1].Text);
    }

    [Fact]
    public async Task Handle_PassesTools()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        Assert.Contains(_sentOptions!.Tools!, t => t.Name == "search_donors");
        Assert.Contains(_sentOptions!.Tools!, t => t.Name == "get_dashboard_stats");
        Assert.Contains(_sentOptions!.Tools!, t => t.Name == "list_relationship_managers");
    }

    [Fact]
    public async Task Handle_ReturnsReplyToolCallsAndModel()
    {
        var command = Command();

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.ConversationId, result.ConversationId);
        Assert.Contains("There are 4 pending donors.", result.Reply);
        Assert.Equal(["search_donors", "get_dashboard_stats"], result.ToolCalls.Select(c => c.Name));
        Assert.Equal("qwen2.5:7b", result.Model);
    }

    [Fact]
    public async Task Handle_PopulatesAuditFields()
    {
        var command = Command();

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("AgentConversation", command.EntityType);
        Assert.Equal(CRM.Domain.Enums.AuditAction.Viewed, command.Action);
        Assert.Equal(command.ConversationId, command.EntityId);
        Assert.Null(command.OldValues);

        // Serialised the same way AuditBehaviour writes it to audit_logs.new_values.
        var audit = JsonDocument.Parse(JsonSerializer.Serialize(command.NewValues)).RootElement;
        Assert.Equal("openai-compatible", audit.GetProperty("provider").GetString());
        Assert.Equal("qwen2.5:7b", audit.GetProperty("model").GetString());
        Assert.Equal("And active?", audit.GetProperty("question").GetString());
        var toolCalls = audit.GetProperty("toolCalls").EnumerateArray().ToList();
        Assert.Equal(["search_donors", "get_dashboard_stats"], toolCalls.Select(t => t.GetProperty("name").GetString()));
        Assert.Equal("Active", toolCalls[0].GetProperty("arguments").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Handle_DoesNotAuditReplyText()
    {
        var command = Command();

        await _handler.Handle(command, CancellationToken.None);

        Assert.DoesNotContain("There are 4 pending donors.", JsonSerializer.Serialize(command.NewValues));
    }

    [Fact]
    public async Task Handle_TruncatesAuditedQuestionTo500Chars()
    {
        var command = Command() with { Messages = [new("user", new string('q', 600))] };

        await _handler.Handle(command, CancellationToken.None);

        var audit = JsonDocument.Parse(JsonSerializer.Serialize(command.NewValues)).RootElement;
        Assert.Equal(500, audit.GetProperty("question").GetString()!.Length);
    }

    [Fact]
    public async Task Handle_FallsBackToConfiguredModelWhenProviderOmitsIt()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi.")));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        Assert.Equal("configured-model", result.Model);
    }

    [Fact]
    public async Task Handle_Disabled_ThrowsAgentUnavailable_WithoutCallingProvider()
    {
        await Assert.ThrowsAsync<AgentUnavailableException>(() => HandlerWith(AgentSettings(enabled: false)).Handle(Command(), CancellationToken.None));

        await _chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task Handle_ProviderError_ThrowsAgentUnavailable(Type exceptionType)
    {
        var providerError = (Exception)Activator.CreateInstance(exceptionType, "429 quota exceeded")!;
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw providerError);

        var ex = await Assert.ThrowsAsync<AgentUnavailableException>(() => _handler.Handle(Command(), CancellationToken.None));

        Assert.Same(providerError, ex.InnerException);
    }

    [Fact]
    public async Task Handle_ProviderTooSlow_ThrowsAgentUnavailable()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
                return new ChatResponse();
            });

        await Assert.ThrowsAsync<AgentUnavailableException>(() => HandlerWith(AgentSettings(timeoutSeconds: 1)).Handle(Command(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CallerCancels_PropagatesCancellation()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
                return new ChatResponse();
            });
        using var aborted = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _handler.Handle(Command(), aborted.Token));
    }
}
