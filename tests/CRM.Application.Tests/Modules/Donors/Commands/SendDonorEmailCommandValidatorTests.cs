using CRM.Application.Modules.Donors.Commands.SendDonorEmail;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class SendDonorEmailCommandValidatorTests
{
    private readonly SendDonorEmailCommandValidator _validator = new();

    private static SendDonorEmailCommand MakeValidCommand() => new()
    {
        DonorId = Guid.NewGuid(),
        To = "donor@example.com",
        Subject = "Update on your donation",
        Body = "Thank you for your continued support."
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(MakeValidCommand());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("donor@example.com,other@example.com")]
    [InlineData("donor@example.com;other@example.com")]
    [InlineData("donor@example.com, other@example.com")]
    public void Validate_MultipleRecipients_FailsWithClearMessage(string to)
    {
        var command = MakeValidCommand();
        command.To = to;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.To) && e.ErrorMessage.Contains("single recipient"));
    }

    [Fact]
    public void Validate_EmptyTo_Fails()
    {
        var command = MakeValidCommand();
        command.To = "";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.To));
    }

    [Fact]
    public void Validate_InvalidEmailFormat_Fails()
    {
        var command = MakeValidCommand();
        command.To = "not-an-email";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.To));
    }

    [Fact]
    public void Validate_EmptySubject_Fails()
    {
        var command = MakeValidCommand();
        command.Subject = "";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.Subject));
    }

    [Fact]
    public void Validate_SubjectTooLong_Fails()
    {
        var command = MakeValidCommand();
        command.Subject = new string('a', 201);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.Subject));
    }

    [Fact]
    public void Validate_SubjectAtMaxLength_Passes()
    {
        var command = MakeValidCommand();
        command.Subject = new string('a', 200);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyBody_Fails()
    {
        var command = MakeValidCommand();
        command.Body = "";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.Body));
    }

    [Fact]
    public void Validate_BodyTooLong_Fails()
    {
        var command = MakeValidCommand();
        command.Body = new string('a', 10_001);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.Body));
    }

    [Fact]
    public void Validate_BodyAtMaxLength_Passes()
    {
        var command = MakeValidCommand();
        command.Body = new string('a', 10_000);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDonorId_Fails()
    {
        var command = MakeValidCommand();
        command.DonorId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SendDonorEmailCommand.DonorId));
    }
}
