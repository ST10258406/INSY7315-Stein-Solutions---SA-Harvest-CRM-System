using CRM.Application.Modules.Interactions.Commands.LogInteraction;

namespace CRM.Application.Tests.Modules.Interactions.Commands;

public class LogInteractionCommandValidatorTests
{
    private readonly LogInteractionCommandValidator _validator = new();

    private static LogInteractionCommand MakeValid() => new()
    {
        DonorId = Guid.NewGuid(),
        InteractionType = "Call",
        Subject = "Intro call",
        Body = "Spoke to the procurement lead.",
        FollowUpDate = null
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(MakeValid()).IsValid);
    }

    [Fact]
    public void Validate_EmptyDonorId_Fails()
    {
        var c = MakeValid();
        c.DonorId = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("call")]
    [InlineData("Note")]
    [InlineData("Email")]
    [InlineData("Meeting")]
    public void Validate_KnownInteractionType_Passes(string type)
    {
        var c = MakeValid();
        c.InteractionType = type;
        Assert.True(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Telepathy")]
    public void Validate_UnknownInteractionType_Fails(string type)
    {
        var c = MakeValid();
        c.InteractionType = type;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingBody_Fails(string body)
    {
        var c = MakeValid();
        c.Body = body;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_FutureFollowUpDate_Passes()
    {
        var c = MakeValid();
        c.FollowUpDate = DateTime.UtcNow.AddDays(3);
        Assert.True(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void Validate_PastOrPresentFollowUpDate_Fails(int secondsFromNow)
    {
        var c = MakeValid();
        c.FollowUpDate = DateTime.UtcNow.AddSeconds(secondsFromNow);
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_NullFollowUpDate_Passes()
    {
        var c = MakeValid();
        c.FollowUpDate = null;
        Assert.True(_validator.Validate(c).IsValid);
    }
}
