using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IEmailService _emailServiceMock = Substitute.For<IEmailService>();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(_usersMock, _unitOfWorkMock, _emailServiceMock);
    }

    private static ForgotPasswordCommand MakeCommand(string email) => new()
    {
        Email = email,
        ResetPasswordUrl = "https://app.example.test/reset-password"
    };

    [Fact]
    public async Task Handle_ExistingEmail_SetsTokenAndExpiry_CallsEmailService_ReturnsGenericMessage()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _usersMock.GetByEmailAsync("existing@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var command = MakeCommand("existing@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);

        Assert.NotNull(user.PasswordResetToken);
        Assert.NotEmpty(user.PasswordResetToken!);
        Assert.NotNull(user.PasswordResetTokenExpiresAt);
        Assert.True(user.PasswordResetTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(50));
        Assert.True(user.PasswordResetTokenExpiresAt <= DateTimeOffset.UtcNow.AddHours(1).AddMinutes(1));

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailServiceMock.Received(1).SendAsync(
            user.Email,
            "Reset your SA Harvest CRM password",
            Arg.Is<string>(body => body.Contains(user.FirstName) && body.Contains("reset-password")),
            EmailType.PasswordReset,
            Arg.Any<Guid?>(),
            user.Id);
    }

    [Fact]
    public async Task Handle_NonExistentEmail_DoesNotCallEmailService_ReturnsGenericMessage()
    {
        // Arrange
        _usersMock.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = MakeCommand("nonexistent@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailServiceMock.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task Handle_NonExistentEmail_StillQueriesTheRepository()
    {
        // Guards the deliberate no-short-circuit design: the lookup must happen for
        // every request so a missing email isn't measurably faster than a real one.
        _usersMock.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await _handler.Handle(MakeCommand("nonexistent@example.com"), CancellationToken.None);

        await _usersMock.Received(1).GetByEmailAsync("nonexistent@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GeneratesUniqueTokensAcrossCalls()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@example.com", FirstName = "User1" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@example.com", FirstName = "User2" };

        _usersMock.GetByEmailAsync("user1@example.com", Arg.Any<CancellationToken>()).Returns(user1);
        _usersMock.GetByEmailAsync("user2@example.com", Arg.Any<CancellationToken>()).Returns(user2);

        // Act
        await _handler.Handle(MakeCommand("user1@example.com"), CancellationToken.None);
        var token1 = user1.PasswordResetToken;

        await _handler.Handle(MakeCommand("user2@example.com"), CancellationToken.None);
        var token2 = user2.PasswordResetToken;

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
    }
}
