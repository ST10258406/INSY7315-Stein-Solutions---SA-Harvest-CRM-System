using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IEmailService _emailServiceMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _emailServiceMock = Substitute.For<IEmailService>();
        _handler = new ForgotPasswordCommandHandler(_contextMock, _emailServiceMock);
    }

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

        var usersList = new List<User> { user };
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        var command = new ForgotPasswordCommand("existing@example.com");

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

        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailServiceMock.Received(1).SendAsync(
            user.Email,
            "Reset your SA Harvest CRM password",
            Arg.Is<string>(body => body.Contains(user.FirstName) && body.Contains("reset-password")));
    }

    [Fact]
    public async Task Handle_NonExistentEmail_DoesNotCallEmailService_ReturnsGenericMessage()
    {
        // Arrange
        var usersList = new List<User>(); // Empty
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        var command = new ForgotPasswordCommand("nonexistent@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("If this email address exists, a reset link has been sent.", result.Message);

        await _contextMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailServiceMock.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_GeneratesUniqueTokensAcrossCalls()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@example.com", FirstName = "User1" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@example.com", FirstName = "User2" };
        var usersList = new List<User> { user1, user2 };
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        // Act
        await _handler.Handle(new ForgotPasswordCommand("user1@example.com"), CancellationToken.None);
        var token1 = user1.PasswordResetToken;

        await _handler.Handle(new ForgotPasswordCommand("user2@example.com"), CancellationToken.None);
        var token2 = user2.PasswordResetToken;

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
    }
}
