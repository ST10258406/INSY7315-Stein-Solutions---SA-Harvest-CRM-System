using CRM.Application.Modules.Auth.Commands.ChangePassword;
using FluentValidation.Results;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyErrors()
    {
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!");
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void PasswordsDoNotMatch_ShouldHaveValidationError()
    {
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "Different123!");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConfirmPassword" && e.ErrorMessage == "Passwords do not match.");
    }

    [Theory]
    [InlineData("short", "short")]
    [InlineData("password", "password")]
    [InlineData("PASSWORD", "PASSWORD")]
    [InlineData("Password123", "Password123")]
    [InlineData("Password!!!", "Password!!!")]
    public void PasswordFailsComplexity_ShouldHaveValidationError(string password, string confirm)
    {
        var command = new ChangePasswordCommand("OldPassword123!", password, confirm);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
    }
}
