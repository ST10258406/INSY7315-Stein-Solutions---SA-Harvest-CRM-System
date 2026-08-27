using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtTokenService _jwtTokenServiceMock = Substitute.For<IJwtTokenService>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(_refreshTokensMock, _jwtTokenServiceMock);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAccessToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "refresh-test@example.com",
            FirstName = "Refresh",
            LastName = "User",
            UserRoles = new List<UserRole>()
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "valid-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _refreshTokensMock.GetByTokenWithUserAndRolesAsync("valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        _jwtTokenServiceMock.GenerateAccessToken(user).Returns("new-access-token");
        _jwtTokenServiceMock.AccessTokenExpirySeconds.Returns(3600);

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "expired-test@example.com" };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "expired-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5), // Expired
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-7)
        };

        _refreshTokensMock.GetByTokenWithUserAndRolesAsync("expired-token", Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var command = new RefreshTokenCommand("expired-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "revoked-test@example.com" };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "revoked-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true, // Revoked
            CreatedAt = DateTimeOffset.UtcNow
        };

        _refreshTokensMock.GetByTokenWithUserAndRolesAsync("revoked-token", Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var command = new RefreshTokenCommand("revoked-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentToken_ThrowsUnauthorizedException()
    {
        // Arrange
        _refreshTokensMock.GetByTokenWithUserAndRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new RefreshTokenCommand("nonexistent-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }
}
