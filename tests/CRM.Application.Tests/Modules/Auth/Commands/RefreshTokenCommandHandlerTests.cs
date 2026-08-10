using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IJwtTokenService _jwtTokenServiceMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _jwtTokenServiceMock = Substitute.For<IJwtTokenService>();
        _handler = new RefreshTokenCommandHandler(_contextMock, _jwtTokenServiceMock);
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

        var tokensList = new List<RefreshToken> { refreshToken };
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        _jwtTokenServiceMock.GenerateAccessToken(user).Returns("new-access-token");
        _jwtTokenServiceMock.AccessTokenExpirySeconds.Returns(3600);

        var command = new RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal(3600, result.ExpiresIn);

        // Verify SaveChangesAsync was NOT called
        await _contextMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "expired-test@example.com"
        };

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

        var tokensList = new List<RefreshToken> { refreshToken };
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new RefreshTokenCommand("expired-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "revoked-test@example.com"
        };

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

        var tokensList = new List<RefreshToken> { refreshToken };
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new RefreshTokenCommand("revoked-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonExistentToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var tokensList = new List<RefreshToken>(); // Empty
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new RefreshTokenCommand("nonexistent-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.Equal("Refresh token is invalid or expired.", ex.Message);
    }
}
