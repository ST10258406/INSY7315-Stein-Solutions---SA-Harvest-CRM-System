using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshTokensMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_ValidToken_IsRevokedBecomesTrue()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "valid-token",
            IsRevoked = false
        };

        _refreshTokensMock.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(refreshToken);

        var command = new LogoutCommand("valid-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(refreshToken.IsRevoked);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_DoesNotSaveAgain()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked-token",
            IsRevoked = true
        };

        _refreshTokensMock.GetByTokenAsync("revoked-token", Arg.Any<CancellationToken>()).Returns(refreshToken);

        var command = new LogoutCommand("revoked-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(refreshToken.IsRevoked); // stays true
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentToken_DoesNotThrow()
    {
        // Arrange
        _refreshTokensMock.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new LogoutCommand("nonexistent-token");

        // Act
        var exception = await Record.ExceptionAsync(() => _handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
