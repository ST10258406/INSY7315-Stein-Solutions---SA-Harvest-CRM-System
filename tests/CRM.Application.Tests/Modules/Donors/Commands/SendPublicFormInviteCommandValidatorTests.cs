using CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class SendPublicFormInviteCommandValidatorTests
{
    private readonly SendPublicFormInviteCommandValidator _validator = new();

    private static SendPublicFormInviteCommand MakeValidCommand() => new()
    {
        To = "prospect@example.com",
        Subject = "Join SA Harvest as a donor",
        Body = "Hi, we'd love to have you on board.",
        PublicFormUrl = "https://app.saharvestcrm.org/donate"
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(MakeValidCommand()).IsValid);
    }

    [Theory]
    [InlineData("a@example.com,b@example.com")]
    [InlineData("a@example.com;b@example.com")]
    public void Validate_MultipleRecipients_FailsWithClearMessage(string to)
    {
        var command = MakeValidCommand();
        command.To = to;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendPublicFormInviteCommand.To) && e.ErrorMessage.Contains("single recipient"));
    }

    [Fact]
    public void Validate_InvalidEmailFormat_Fails()
    {
        var command = MakeValidCommand();
        command.To = "not-an-email";

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptySubject_Fails()
    {
        var command = MakeValidCommand();
        command.Subject = "";

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_SubjectTooLong_Fails()
    {
        var command = MakeValidCommand();
        command.Subject = new string('a', 201);

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_EmptyBody_Fails()
    {
        var command = MakeValidCommand();
        command.Body = "";

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_BodyTooLong_Fails()
    {
        var command = MakeValidCommand();
        command.Body = new string('a', 10_001);

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_MissingPublicFormUrl_Fails()
    {
        var command = MakeValidCommand();
        command.PublicFormUrl = "";

        Assert.False(_validator.Validate(command).IsValid);
    }
}
