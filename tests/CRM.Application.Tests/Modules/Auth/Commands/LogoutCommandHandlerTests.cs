using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class LogoutCommandHandlerTests
{
    private const string RawToken = "logout-refresh-token";

    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly LogoutCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public LogoutCommandHandlerTests()
    {
        _currentUserServiceMock.GetCurrentUserId().Returns(_currentUserId);
        _handler = new LogoutCommandHandler(_refreshTokensMock, _unitOfWorkMock, _currentUserServiceMock);
    }

    private RefreshToken StoreToken(Guid ownerId, bool isRevoked = false)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            TokenHash = SecureTokens.Hash(RawToken),
            FamilyId = Guid.NewGuid(),
            IsRevoked = isRevoked,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _refreshTokensMock.GetByHashWithUserAndRolesAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _refreshTokensMock.GetActiveByFamilyIdAsync(token.FamilyId, Arg.Any<CancellationToken>())
            .Returns(isRevoked ? new List<RefreshToken>() : new List<RefreshToken> { token });
        return token;
    }

    [Fact]
    public async Task Handle_OwnToken_RevokesItsFamily()
    {
        var token = StoreToken(_currentUserId);

        await _handler.Handle(new LogoutCommand(RawToken), CancellationToken.None);

        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnotherUsersToken_IsLeftAlone()
    {
        // F-21: a caller must never be able to end someone else's session.
        var token = StoreToken(ownerId: Guid.NewGuid());

        await _handler.Handle(new LogoutCommand(RawToken), CancellationToken.None);

        Assert.False(token.IsRevoked);
        await _refreshTokensMock.DidNotReceive().GetActiveByFamilyIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_DoesNotSaveAgain()
    {
        StoreToken(_currentUserId, isRevoked: true);

        await _handler.Handle(new LogoutCommand(RawToken), CancellationToken.None);

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentToken_DoesNotThrow()
    {
        _refreshTokensMock.GetByHashWithUserAndRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        await _handler.Handle(new LogoutCommand("unknown"), CancellationToken.None);

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_NoCookie_DoesNothing(string? rawToken)
    {
        await _handler.Handle(new LogoutCommand(rawToken), CancellationToken.None);

        await _refreshTokensMock.DidNotReceive().GetByHashWithUserAndRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
