using CRM.Application.Modules.Tasks.Commands.UpdateTask;

namespace CRM.Application.Tests.Modules.Tasks.Commands;

public class UpdateTaskCommandValidatorTests
{
    private readonly UpdateTaskCommandValidator _validator = new();

    private static UpdateTaskCommand MakeValid() => new() { Id = Guid.NewGuid() };

    [Fact]
    public void Validate_EmptyPatch_Passes()  // nothing to change is still a valid (no-op) request
        => Assert.True(_validator.Validate(MakeValid()).IsValid);

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var c = MakeValid();
        c.Id = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ProvidedButBlankTitle_Fails(string title)
    {
        var c = MakeValid();
        c.Title = title;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_ProvidedValidTitle_Passes()
    {
        var c = MakeValid();
        c.Title = "Updated title";
        Assert.True(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_ProvidedEmptyAssignee_Fails()
    {
        var c = MakeValid();
        c.AssignedToUserId = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_PastDueDate_Fails()
    {
        var c = MakeValid();
        c.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_FutureDueDate_Passes()
    {
        var c = MakeValid();
        c.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        Assert.True(_validator.Validate(c).IsValid);
    }
}
