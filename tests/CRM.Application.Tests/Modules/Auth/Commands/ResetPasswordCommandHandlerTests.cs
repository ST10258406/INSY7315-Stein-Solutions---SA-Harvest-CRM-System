using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ResetPasswordCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _handler = new ResetPasswordCommandHandler(_contextMock);
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

        var usersList = new List<User> { user };
        var mockUserDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUserDbSet);

        var refreshToken1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IsRevoked = false
        };
        var refreshToken2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IsRevoked = true // already revoked
        };
        var tokensList = new List<RefreshToken> { refreshToken1, refreshToken2 };
        var mockTokenDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockTokenDbSet);

        var command = new ResetPasswordCommand("valid-reset-token", "reset-happy@example.com", "NewPassword123!", "NewPassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Password has been reset successfully.", result.Message);
        
        Assert.NotEqual("old-hash", user.PasswordHash); // Password hashed
        Assert.Null(user.PasswordResetToken); // Token burned
        Assert.Null(user.PasswordResetTokenExpiresAt);
        
        Assert.True(refreshToken1.IsRevoked); // Active token got revoked
        Assert.True(refreshToken2.IsRevoked); // Already revoked token stays revoked

        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var usersList = new List<User> { user };
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ResetPasswordCommand("expired-token", "expired@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
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

        var usersList = new List<User> { user };
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ResetPasswordCommand("wrong-token", "wrong-token@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ThrowsValidationException()
    {
        // Arrange
        var usersList = new List<User>(); // Empty
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ResetPasswordCommand("token", "missing@example.com", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Token expired or invalid.", ex.Errors["Token"]);
    }
}
