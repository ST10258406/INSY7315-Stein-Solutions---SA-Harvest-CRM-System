using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(_usersMock, _refreshTokensMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_ValidCommand_ResetsPasswordAndRevokesTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "reset-happy@example.com",
            PasswordResetToken = "valid-reset-token",
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            PasswordHash = "old-hash"
        };

        _usersMock.GetByEmailAsync("reset-happy@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var refreshToken1 = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, IsRevoked = false };
        var refreshToken2 = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, IsRevoked = false };

        _refreshTokensMock.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([refreshToken1, refreshToken2]);

        var command = new ResetPasswordCommand("valid-reset-token", "reset-happy@example.com", "NewPassword123!", "NewPassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Password has been reset successfully.", result.Message);

        Assert.NotEqual("old-hash", user.PasswordHash); // Password hashed
        Assert.Null(user.PasswordResetToken);           // Token burned
        Assert.Null(user.PasswordResetTokenExpiresAt);

        Assert.True(refreshToken1.IsRevoked);
        Assert.True(refreshToken2.IsRevoked);

        // CRITICAL: the user mutation and the refresh-token revocations must commit
        // together. Exactly one SaveChangesAsync call covers both — a second call
        // here would mean the two mutations are no longer in a single transaction.
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesOnceEvenWithNoActiveTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "no-tokens@example.com",
            PasswordResetToken = "valid-reset-token",
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            PasswordHash = "old-hash"
        };

        _usersMock.GetByEmailAsync("no-tokens@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokensMock.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);

        await _handler.Handle(
            new ResetPasswordCommand("valid-reset-token", "no-tokens@example.com", "NewPassword123!", "NewPassword123!"),
            CancellationToken.None);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsValidationException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "expired@example.com",
            PasswordResetToken = "expired-token",
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        _usersMock.GetByEmailAsync("expired@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var command = new ResetPasswordCommand("expired-token", "expired@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongToken_ThrowsValidationException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrong-token@example.com",
            PasswordResetToken = "correct-token",
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        _usersMock.GetByEmailAsync("wrong-token@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var command = new ResetPasswordCommand("wrong-token", "wrong-token@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ThrowsValidationException()
    {
        // Arrange
        _usersMock.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new ResetPasswordCommand("token", "missing@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
    }
}
