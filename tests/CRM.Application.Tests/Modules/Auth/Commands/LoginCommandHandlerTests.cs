using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IJwtTokenService _jwtTokenServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _jwtTokenServiceMock = Substitute.For<IJwtTokenService>();
        _handler = new LoginCommandHandler(_contextMock, _jwtTokenServiceMock);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndUser_AndSavesRefreshToken()
    {
        // Arrange
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = passwordHasher.HashPassword(null!, "CorrectPassword"),
            UserRoles = new List<UserRole>()
        };

        var usersList = new List<User> { user };
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        var mockRefreshTokensDbSet = (new List<RefreshToken>()).BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockRefreshTokensDbSet);

        _jwtTokenServiceMock.GenerateAccessToken(user).Returns("access-token");
        _jwtTokenServiceMock.GenerateRefreshToken().Returns("refresh-token");

        var command = new LoginCommand("test@example.com", "CorrectPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(user.Id, result.User.Id);

        // Verify that a refresh token was added to the DbSet
        mockRefreshTokensDbSet.Received(1).Add(Arg.Is<RefreshToken>(rt => rt.UserId == user.Id && rt.Token == "refresh-token"));
        
        // Verify SaveChangesAsync was called
        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = passwordHasher.HashPassword(null!, "CorrectPassword"),
            UserRoles = new List<UserRole>()
        };

        var usersList = new List<User> { user };
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        var command = new LoginCommand("test@example.com", "WrongPassword");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var usersList = new List<User>(); // Empty
        var mockUsersDbSet = usersList.BuildMockDbSet();
        _contextMock.Users.Returns(mockUsersDbSet);

        var command = new LoginCommand("wrong@example.com", "SomePassword");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid email or password.", ex.Message); // Same exception message!
    }
}
