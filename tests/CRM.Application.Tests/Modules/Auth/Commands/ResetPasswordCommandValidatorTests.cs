using CRM.Application.Modules.Auth.Commands.ResetPassword;
using FluentValidation.Results;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyErrors()
    {
        var command = new ResetPasswordCommand("token", "test@example.com", "Password123!", "Password123!");
        var result = _validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void PasswordsDoNotMatch_ShouldHaveValidationError()
    {
        var command = new ResetPasswordCommand("token", "test@example.com", "Password123!", "Different123!");
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConfirmPassword" && e.ErrorMessage == "Passwords do not match.");
    }

    [Theory]
    [InlineData("short", "short")] // Too short, no upper, no special, no digit
    [InlineData("password", "password")] // No upper, no special, no digit
    [InlineData("PASSWORD", "PASSWORD")] // No lower (but validator only checks upper), no digit, no special
    [InlineData("Password123", "Password123")] // No special
    [InlineData("Password!!!", "Password!!!")] // No digit
    public void PasswordFailsComplexity_ShouldHaveValidationError(string password, string confirm)
    {
        var command = new ResetPasswordCommand("token", "test@example.com", password, confirm);
        var result = _validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
    }
}

