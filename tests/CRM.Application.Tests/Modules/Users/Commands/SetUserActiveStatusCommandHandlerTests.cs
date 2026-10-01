using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.SetUserActiveStatus;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class SetUserActiveStatusCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly SetUserActiveStatusCommandHandler _handler;

    public SetUserActiveStatusCommandHandlerTests()
    {
        _handler = new SetUserActiveStatusCommandHandler(_usersMock, _refreshTokensMock, _unitOfWorkMock, _currentUserServiceMock);
    }

    [Fact]
    public async Task Handle_DeactivateOwnAccount_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.GetCurrentUserId().Returns(userId);

        var command = new SetUserActiveStatusCommand { Id = userId, IsActive = false };

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(command, CancellationToken.None));
        await _usersMock.DidNotReceive().GetByIdWithRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _currentUserServiceMock.GetCurrentUserId().Returns(currentUserId);
        _usersMock.GetByIdWithRoleAsync(targetUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new SetUserActiveStatusCommand { Id = targetUserId, IsActive = false };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeactivateOtherUser_SetsIsActiveFalse()
    {
        var currentUserId = Guid.NewGuid();
        var targetUser = new User { Id = Guid.NewGuid(), FirstName = "Johan", LastName = "Pretorius", Email = "johan@saharvest.org", IsActive = true };
        _currentUserServiceMock.GetCurrentUserId().Returns(currentUserId);
        _usersMock.GetByIdWithRoleAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _refreshTokensMock.GetActiveByUserIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(new List<RefreshToken>());

        var command = new SetUserActiveStatusCommand { Id = targetUser.Id, IsActive = false };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.False(targetUser.IsActive);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivateOtherUser_RevokesTheirActiveRefreshTokens()
    {
        // A deactivated account must lose access immediately, not just be blocked from
        // future logins — an outstanding refresh token would otherwise keep working.
        var currentUserId = Guid.NewGuid();
        var targetUser = new User { Id = Guid.NewGuid(), FirstName = "Johan", LastName = "Pretorius", Email = "johan@saharvest.org", IsActive = true };
        var activeTokens = new List<RefreshToken>
        {
            new() { Id = Guid.NewGuid(), UserId = targetUser.Id, TokenHash = "t1", IsRevoked = false, ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) },
            new() { Id = Guid.NewGuid(), UserId = targetUser.Id, TokenHash = "t2", IsRevoked = false, ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) }
        };
        _currentUserServiceMock.GetCurrentUserId().Returns(currentUserId);
        _usersMock.GetByIdWithRoleAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _refreshTokensMock.GetActiveByUserIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(activeTokens);

        await _handler.Handle(new SetUserActiveStatusCommand { Id = targetUser.Id, IsActive = false }, CancellationToken.None);

        Assert.All(activeTokens, t => Assert.True(t.IsRevoked));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReactivateUser_SetsIsActiveTrue()
    {
        var currentUserId = Guid.NewGuid();
        var targetUser = new User { Id = Guid.NewGuid(), FirstName = "Annelie", LastName = "du Toit", Email = "annelie@saharvest.org", IsActive = false };
        _currentUserServiceMock.GetCurrentUserId().Returns(currentUserId);
        _usersMock.GetByIdWithRoleAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);

        var command = new SetUserActiveStatusCommand { Id = targetUser.Id, IsActive = true };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsActive);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Reactivating doesn't touch refresh tokens — only deactivation revokes them.
        await _refreshTokensMock.DidNotReceive().GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
