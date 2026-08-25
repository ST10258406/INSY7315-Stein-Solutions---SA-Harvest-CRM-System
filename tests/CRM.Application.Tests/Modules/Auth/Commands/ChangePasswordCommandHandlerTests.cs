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
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly ChangePasswordCommandHandler _handler;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(_usersMock, _unitOfWorkMock, _currentUserServiceMock);
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
