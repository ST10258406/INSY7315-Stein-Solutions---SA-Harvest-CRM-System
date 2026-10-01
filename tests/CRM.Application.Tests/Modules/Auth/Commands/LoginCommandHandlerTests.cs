using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IJwtTokenService _jwtTokenServiceMock = Substitute.For<IJwtTokenService>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _usersMock, _refreshTokensMock, _unitOfWorkMock, _jwtTokenServiceMock,
            Options.Create(new LoginLockoutOptions { MaxFailedAttempts = 5, LockoutMinutes = 15 }),
            NullLogger<LoginCommandHandler>.Instance);
    }

    private User SeedUser(string password = "CorrectPassword", int failedLoginCount = 0, DateTimeOffset? lockoutEndUtc = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "lockout@example.com",
            FirstName = "Lock",
            LastName = "Out",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            FailedLoginCount = failedLoginCount,
            LockoutEndUtc = lockoutEndUtc,
            UserRoles = new List<UserRole>()
        };
        _usersMock.GetByEmailWithRolesAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenServiceMock.GenerateRefreshToken().Returns("refresh-token");
        return user;
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

        _usersMock.GetByEmailWithRolesAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(user);

        _jwtTokenServiceMock.GenerateAccessToken(user).Returns("access-token");
        _jwtTokenServiceMock.GenerateRefreshToken().Returns("refresh-token");
        _jwtTokenServiceMock.AccessTokenExpirySeconds.Returns(3600);

        var command = new LoginCommand("test@example.com", "CorrectPassword");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal(user.Id, result.User.Id);

        await _refreshTokensMock.Received(1).AddAsync(
            Arg.Is<RefreshToken>(rt => rt.UserId == user.Id && rt.Token == "refresh-token" && !rt.IsRevoked),
            Arg.Any<CancellationToken>());

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCredentials_ProjectsRoleNames()
    {
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "roles@example.com",
            FirstName = "Role",
            LastName = "Holder",
            PasswordHash = passwordHasher.HashPassword(null!, "CorrectPassword"),
            UserRoles = new List<UserRole>
            {
                new() { Role = new Role { Name = "Admin" } }
            }
        };

        _usersMock.GetByEmailWithRolesAsync("roles@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _jwtTokenServiceMock.GenerateRefreshToken().Returns("refresh-token");

        var result = await _handler.Handle(new LoginCommand("roles@example.com", "CorrectPassword"), CancellationToken.None);

        Assert.Equal(["Admin"], result.User.Roles);
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

        _usersMock.GetByEmailWithRolesAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var command = new LoginCommand("test@example.com", "WrongPassword");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid email or password.", ex.Message);
        // The failed attempt is counted (and persisted) towards the lockout threshold.
        Assert.Equal(1, user.FailedLoginCount);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorizedException()
    {
        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "deactivated@example.com",
            PasswordHash = passwordHasher.HashPassword(null!, "CorrectPassword"),
            IsActive = false,
            UserRoles = new List<UserRole>()
        };

        _usersMock.GetByEmailWithRolesAsync("deactivated@example.com", Arg.Any<CancellationToken>()).Returns(user);

        var command = new LoginCommand("deactivated@example.com", "CorrectPassword");

        // Same generic message as "wrong password" / "no such user" — a deactivated
        // account must not be distinguishable from any other login failure.
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid email or password.", ex.Message);
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        _usersMock.GetByEmailWithRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new LoginCommand("wrong@example.com", "SomePassword");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Invalid email or password.", ex.Message); // Same exception message!
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---- F-02: per-account lockout ----

    [Fact]
    public async Task Handle_FifthConsecutiveFailure_LocksAccountFor15Minutes()
    {
        var user = SeedUser(failedLoginCount: 4);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new LoginCommand(user.Email, "WrongPassword"), CancellationToken.None));

        Assert.NotNull(user.LockoutEndUtc);
        Assert.InRange(user.LockoutEndUtc!.Value, DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(16));
        // Counter restarts so the account gets a fresh set of tries once the lock expires.
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public async Task Handle_FourthConsecutiveFailure_DoesNotLock()
    {
        var user = SeedUser(failedLoginCount: 3);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new LoginCommand(user.Email, "WrongPassword"), CancellationToken.None));

        Assert.Null(user.LockoutEndUtc);
        Assert.Equal(4, user.FailedLoginCount);
    }

    [Fact]
    public async Task Handle_LockedAccount_RejectsCorrectPasswordWithGenericMessage()
    {
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        var user = SeedUser(lockoutEndUtc: lockoutEnd);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new LoginCommand(user.Email, "CorrectPassword"), CancellationToken.None));

        // Indistinguishable from a wrong password, so lockout isn't an enumeration oracle.
        Assert.Equal("Invalid email or password.", ex.Message);
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedAttemptWhileLocked_DoesNotExtendLockOrCount()
    {
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        var user = SeedUser(lockoutEndUtc: lockoutEnd);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new LoginCommand(user.Email, "WrongPassword"), CancellationToken.None));

        Assert.Equal(lockoutEnd, user.LockoutEndUtc);
        Assert.Equal(0, user.FailedLoginCount);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredLock_CorrectPasswordSucceedsAndClearsLock()
    {
        var user = SeedUser(lockoutEndUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await _handler.Handle(new LoginCommand(user.Email, "CorrectPassword"), CancellationToken.None);

        Assert.Equal(user.Id, result.User.Id);
        Assert.Null(user.LockoutEndUtc);
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ResetsFailedCount()
    {
        var user = SeedUser(failedLoginCount: 3);

        await _handler.Handle(new LoginCommand(user.Email, "CorrectPassword"), CancellationToken.None);

        Assert.Equal(0, user.FailedLoginCount);
    }
}
