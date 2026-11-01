namespace CRM.Application.Modules.Agent.Commands.SendAgentMessage;

using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Agent.Tools;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

/// <summary>
/// Sends the conversation to the configured <see cref="IChatClient"/> with the read-only
/// CRM tools attached. The client is wrapped in FunctionInvokingChatClient (see
/// Infrastructure AddAgent), so tool rounds run inside GetResponseAsync and the
/// function calls made come back in the response messages.
///
/// Every way the agent can fail to answer (disabled, provider error or 429, timeout) becomes
/// <see cref="AgentUnavailableException"/> → 503 AGENT_UNAVAILABLE. Tool exceptions don't reach
/// here: FunctionInvokingChatClient reports them to the model as a failed call instead.
/// </summary>
public class SendAgentMessageCommandHandler : IRequestHandler<SendAgentMessageCommand, AgentReplyDto>
{
    private static readonly TimeZoneInfo Sast = TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");

    private const int MaxAuditedQuestionLength = 500;

    private readonly IChatClient _chatClient;
    private readonly CrmAgentTools _tools;
    private readonly AgentOptions _options;

    public SendAgentMessageCommandHandler(IChatClient chatClient, CrmAgentTools tools, IOptions<AgentOptions> options)
    {
        _chatClient = chatClient;
        _tools = tools;
        _options = options.Value;
    }

    public async Task<AgentReplyDto> Handle(SendAgentMessageCommand request, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            throw new AgentUnavailableException("The agent is disabled (Agent:Enabled = false).");

        var todaySast = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Sast));

        List<ChatMessage> messages =
        [
            new(ChatRole.System, AgentSystemPrompt.Build(todaySast)),
            .. request.Messages.Select(m => new ChatMessage(m.Role == "user" ? ChatRole.User : ChatRole.Assistant, m.Content)),
        ];

        // One budget for the whole request, including every tool round.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        ChatResponse response;
        try
        {
            response = await _chatClient.GetResponseAsync(messages, new ChatOptions { Tools = _tools.AsTools() }, timeout.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Our timeout (or the provider HttpClient's) fired; a caller abort propagates unchanged.
            throw new AgentUnavailableException($"The LLM provider did not answer within {_options.TimeoutSeconds}s.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new AgentUnavailableException($"The LLM provider request failed: {ex.Message}", ex);
        }

        var functionCalls = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();
        var model = response.ModelId ?? _options.Model;

        // Read by AuditBehaviour after this returns. The reply text is deliberately not stored:
        // it is derived data and would duplicate PII into audit_logs.
        var question = request.Messages[^1].Content;
        request.EntityId = request.ConversationId;
        request.NewValues = new
        {
            provider = _options.Provider,
            model,
            question = question.Length > MaxAuditedQuestionLength ? question[..MaxAuditedQuestionLength] : question,
            toolCalls = functionCalls.Select(c => new { name = c.Name, arguments = c.Arguments }).ToList(),
        };

        return new AgentReplyDto(
            request.ConversationId,
            response.Text,
            functionCalls.Select(c => new AgentToolCallDto(c.Name)).ToList(),
            model);
    }
}
