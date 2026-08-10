using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class ChangePasswordCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly ChangePasswordCommandHandler _handler;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ChangePasswordCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _handler = new ChangePasswordCommandHandler(_contextMock, _currentUserServiceMock);
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

        var usersList = new List<User> { user };
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword123!", "NewPassword123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "NewPassword123!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);

        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var usersList = new List<User> { user };
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ChangePasswordCommand("WrongPassword123!", "NewPassword123!", "NewPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("Current password is incorrect.", ex.Errors["CurrentPassword"]);
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

        var usersList = new List<User> { user };
        var mockDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockDbSet);

        var command = new ChangePasswordCommand("OldPassword123!", "OldPassword123!", "OldPassword123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Contains("New password cannot be the same as the current password.", ex.Errors["NewPassword"]);
    }
}
