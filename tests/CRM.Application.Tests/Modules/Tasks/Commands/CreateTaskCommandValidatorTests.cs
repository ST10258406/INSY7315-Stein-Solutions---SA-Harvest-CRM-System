using CRM.Application.Modules.Tasks.Commands.CreateTask;

namespace CRM.Application.Tests.Modules.Tasks.Commands;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    private static CreateTaskCommand MakeValid() => new()
    {
        DonorId = Guid.NewGuid(),
        Title = "Follow up on August donation",
        Description = "Confirm pickup schedule",
        AssignedToUserId = Guid.NewGuid(),
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3)
    };

    [Fact]
    public void Validate_ValidCommand_Passes() => Assert.True(_validator.Validate(MakeValid()).IsValid);

    [Fact]
    public void Validate_EmptyDonorId_Fails()
    {
        var c = MakeValid();
        c.DonorId = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingTitle_Fails(string title)
    {
        var c = MakeValid();
        c.Title = title;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_MissingAssignee_Fails()
    {
        var c = MakeValid();
        c.AssignedToUserId = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_DueDateToday_Passes()
    {
        var c = MakeValid();
        c.DueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.True(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_DueDateInThePast_Fails()
    {
        var c = MakeValid();
        c.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Fact]
    public void Validate_NullDescription_Passes()
    {
        var c = MakeValid();
        c.Description = null;
        Assert.True(_validator.Validate(c).IsValid);
    }
}
