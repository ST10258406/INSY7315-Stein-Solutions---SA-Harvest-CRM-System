using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Domain.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class LogoutCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _handler = new LogoutCommandHandler(_contextMock);
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

        var tokensList = new List<RefreshToken> { refreshToken };
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new LogoutCommand("valid-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(refreshToken.IsRevoked);
        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        var tokensList = new List<RefreshToken> { refreshToken };
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new LogoutCommand("revoked-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(refreshToken.IsRevoked); // stays true
        await _contextMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentToken_DoesNotThrow()
    {
        // Arrange
        var tokensList = new List<RefreshToken>();
        var mockDbSet = tokensList.BuildMockDbSet();
        _contextMock.RefreshTokens.Returns(mockDbSet);

        var command = new LogoutCommand("nonexistent-token");

        // Act
        var exception = await Record.ExceptionAsync(() => _handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        await _contextMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
