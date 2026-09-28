using CRM.Application.Modules.Users.Commands.SetUserActiveStatus;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class SetUserActiveStatusCommandValidatorTests
{
    private readonly SetUserActiveStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ExplicitIsActive_Passes(bool isActive)
        => Assert.True(_validator.Validate(new SetUserActiveStatusCommand { Id = Guid.NewGuid(), IsActive = isActive }).IsValid);

    [Fact]
    public void Validate_MissingIsActive_Fails()
    {
        // A body of {} must be rejected, never treated as "deactivate".
        var result = _validator.Validate(new SetUserActiveStatusCommand { Id = Guid.NewGuid(), IsActive = null });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SetUserActiveStatusCommand.IsActive));
    }

    [Fact]
    public void Validate_EmptyId_Fails()
        => Assert.False(_validator.Validate(new SetUserActiveStatusCommand { Id = Guid.Empty, IsActive = true }).IsValid);
}
