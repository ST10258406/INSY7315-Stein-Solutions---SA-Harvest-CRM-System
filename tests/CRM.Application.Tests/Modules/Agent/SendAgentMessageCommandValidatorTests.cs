using CRM.Application.Modules.Agent;
using CRM.Application.Modules.Agent.Commands.SendAgentMessage;
using Microsoft.Extensions.Options;

namespace CRM.Application.Tests.Modules.Agent;

public class SendAgentMessageCommandValidatorTests
{
    private readonly SendAgentMessageCommandValidator _validator =
        new(Options.Create(new AgentOptions { MaxHistoryMessages = 3, MaxMessageLength = 10, MaxAssistantMessageLength = 20 }));

    private static SendAgentMessageCommand Make(params AgentMessageDto[] messages) =>
        new() { ConversationId = Guid.NewGuid(), Messages = messages };

    private static AgentMessageDto User(string content = "hi") => new("user", content);

    [Fact]
    public void Validate_ValidConversation_Passes() =>
        Assert.True(_validator.Validate(Make(User(), new("assistant", "hello"), User())).IsValid);

    [Fact]
    public void Validate_EmptyConversationId_Fails() =>
        Assert.False(_validator.Validate(Make(User()) with { ConversationId = Guid.Empty }).IsValid);

    [Fact]
    public void Validate_NoMessages_Fails() => Assert.False(_validator.Validate(Make()).IsValid);

    [Fact]
    public void Validate_TooManyMessages_Fails() =>
        Assert.False(_validator.Validate(Make(User(), User(), User(), User())).IsValid);

    [Theory]
    [InlineData("system")]
    [InlineData("tool")]
    [InlineData("User")]
    public void Validate_DisallowedRole_Fails(string role) =>
        Assert.False(_validator.Validate(Make(new(role, "x"), User())).IsValid);

    [Fact]
    public void Validate_LastMessageNotUser_Fails() =>
        Assert.False(_validator.Validate(Make(User(), new("assistant", "hello"))).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("01234567890")]
    public void Validate_BadContentLength_Fails(string content) =>
        Assert.False(_validator.Validate(Make(User(content))).IsValid);

    // A long earlier reply must not break the conversation (only user input has the tight limit).
    [Fact]
    public void Validate_AssistantReplyLongerThanUserLimit_Passes() =>
        Assert.True(_validator.Validate(Make(User(), new("assistant", new string('a', 20)), User())).IsValid);

    [Fact]
    public void Validate_AssistantReplyOverItsLimit_Fails() =>
        Assert.False(_validator.Validate(Make(User(), new("assistant", new string('a', 21)), User())).IsValid);

    [Fact]
    public void Validate_NullMessage_FailsWithoutThrowing()
    {
        Assert.False(_validator.Validate(Make(new AgentMessageDto[] { null! })).IsValid);
        Assert.False(_validator.Validate(Make(null!, User())).IsValid);
    }
}
