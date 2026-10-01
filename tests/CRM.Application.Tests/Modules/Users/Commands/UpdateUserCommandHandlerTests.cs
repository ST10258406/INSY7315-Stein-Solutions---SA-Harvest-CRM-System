using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.UpdateUser;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IEmailService _emailServiceMock = Substitute.For<IEmailService>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _refreshTokensMock.GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<RefreshToken>());
        _handler = new UpdateUserCommandHandler(
            _usersMock, _refreshTokensMock, _unitOfWorkMock, _emailServiceMock, _currentUserServiceMock);
    }

    private static UpdateUserRequest NewDetails() =>
        new() { FirstName = "Annelie", LastName = "Botha", Email = "annelie.botha@saharvest.org" };

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _usersMock.GetByIdWithRoleAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateUserCommand { Id = id, Request = NewDetails() }, CancellationToken.None));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesDetailsAndReturnsCurrentRole()
    {
        var oldRole = new Role { Id = Guid.NewGuid(), Name = "Marketing" };
        var currentRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var user = new User { Id = Guid.NewGuid(), FirstName = "Annelie", LastName = "du Toit", Email = "annelie@saharvest.org", IsActive = true };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = oldRole.Id, Role = oldRole, AssignedAt = DateTime.UtcNow.AddDays(-10) });
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = currentRole.Id, Role = currentRole, AssignedAt = DateTime.UtcNow.AddDays(-1) });
        _usersMock.GetByIdWithRoleAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new UpdateUserCommand { Id = user.Id, Request = NewDetails() }, CancellationToken.None);

        Assert.Equal("Botha", user.LastName);
        Assert.Equal("annelie.botha@saharvest.org", user.Email);
        Assert.Equal("Botha", result.LastName);
        Assert.Equal("annelie.botha@saharvest.org", result.Email);
        // Most recently assigned role wins.
        Assert.Equal(currentRole.Id, result.RoleId);
        Assert.Equal("Admin", result.Role);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PopulatesAuditOldAndNewValues()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Annelie", LastName = "du Toit", Email = "annelie@saharvest.org", IsActive = true };
        _usersMock.GetByIdWithRoleAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var command = new UpdateUserCommand { Id = user.Id, Request = NewDetails() };
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(user.Id, command.EntityId);
        Assert.Contains("annelie@saharvest.org", command.OldValues!.ToString());
        Assert.Contains("annelie.botha@saharvest.org", command.NewValues!.ToString());
    }

    [Fact]
    public async Task Handle_EmailChanged_RevokesAllRefreshTokensAndNotifiesOldAddress()
    {
        var actingAdminId = Guid.NewGuid();
        _currentUserServiceMock.GetCurrentUserId().Returns(actingAdminId);
        var user = new User { Id = Guid.NewGuid(), FirstName = "Annelie", LastName = "du Toit", Email = "annelie@saharvest.org", IsActive = true };
        _usersMock.GetByIdWithRoleAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var tokens = new List<RefreshToken>
        {
            new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "a", IsRevoked = false },
            new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "b", IsRevoked = false }
        };
        _refreshTokensMock.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(tokens);

        await _handler.Handle(new UpdateUserCommand { Id = user.Id, Request = NewDetails() }, CancellationToken.None);

        Assert.All(tokens, t => Assert.True(t.IsRevoked));
        await _emailServiceMock.Received(1).SendAsync(
            "annelie@saharvest.org",
            Arg.Any<string>(),
            Arg.Is<string>(body => !body.Contains("annelie.botha@saharvest.org")),
            EmailType.AccountEmailChanged,
            null,
            actingAdminId);
    }

    [Fact]
    public async Task Handle_EmailUnchanged_DoesNotRevokeTokensOrSendNotice()
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Annelie", LastName = "du Toit", Email = "annelie.botha@saharvest.org", IsActive = true };
        _usersMock.GetByIdWithRoleAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new UpdateUserCommand { Id = user.Id, Request = NewDetails() }, CancellationToken.None);

        await _refreshTokensMock.DidNotReceive().GetActiveByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailServiceMock.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
