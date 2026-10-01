using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly ChangePasswordCommandHandler _handler;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ChangePasswordCommandHandlerTests()
    {
        _refreshTokensMock.GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<RefreshToken>());
        _handler = new ChangePasswordCommandHandler(_usersMock, _refreshTokensMock, _unitOfWorkMock, _currentUserServiceMock);
    }

    [Fact]
    public async Task Handle_ValidCommand_ClearsMustChangePassword()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "forced@example.com", MustChangePassword = true, PasswordHash = _passwordHasher.HashPassword(null!, "TempPassword123!") };
        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ChangePasswordCommand("TempPassword123!", "NewPassword123!", "NewPassword123!"), CancellationToken.None);

        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public async Task Handle_ValidCommand_RevokesOtherSessionsButKeepsCurrentOne()
    {
        // F-05: if the old password was compromised, the attacker's sessions must not
        // survive the change — but the caller shouldn't be logged out of the session they
        // are using.
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "sessions@example.com", PasswordHash = _passwordHasher.HashPassword(null!, "OldPassword123!") };
        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var currentFamily = Guid.NewGuid();
        var current = new RefreshToken { UserId = userId, FamilyId = currentFamily, TokenHash = CRM.Application.Common.Utilities.SecureTokens.Hash("my-cookie") };
        var otherDevice = new RefreshToken { UserId = userId, FamilyId = Guid.NewGuid(), TokenHash = "other-device" };
        var attacker = new RefreshToken { UserId = userId, FamilyId = Guid.NewGuid(), TokenHash = "attacker" };
        _refreshTokensMock.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { current, otherDevice, attacker });

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!") { CurrentRefreshToken = "my-cookie" };
        await _handler.Handle(command, CancellationToken.None);

        Assert.False(current.IsRevoked);
        Assert.True(otherDevice.IsRevoked);
        Assert.True(attacker.IsRevoked);
    }

    [Fact]
    public async Task Handle_NoCurrentCookie_RevokesEverySession()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "nocookie@example.com", PasswordHash = _passwordHasher.HashPassword(null!, "OldPassword123!") };
        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var session = new RefreshToken { UserId = userId, FamilyId = Guid.NewGuid(), TokenHash = "some-session" };
        _refreshTokensMock.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<RefreshToken> { session });

        await _handler.Handle(new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!"), CancellationToken.None);

        Assert.True(session.IsRevoked);
    }

    [Fact]
    public async Task Handle_ValidCommand_ResetsPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "change-happy@example.com",
            PasswordHash = _passwordHasher.HashPassword(null!, "OldPassword123!")
        };

        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "NewPassword123!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = _passwordHasher.HashPassword(null!, "OldPassword123!")
        };

        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new ChangePasswordCommand("WrongPassword123!", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Current password is incorrect.", ex.Errors["CurrentPassword"]);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewPasswordSameAsCurrent_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = _passwordHasher.HashPassword(null!, "OldPassword123!")
        };

        _currentUserServiceMock.GetCurrentUserId().Returns(userId);
        _usersMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new ChangePasswordCommand("OldPassword123!", "OldPassword123!", "OldPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("New password cannot be the same as the current password.", ex.Errors["NewPassword"]);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
