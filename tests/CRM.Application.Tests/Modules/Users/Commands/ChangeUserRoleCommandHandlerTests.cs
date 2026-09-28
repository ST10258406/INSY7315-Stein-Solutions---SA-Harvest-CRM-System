using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.ChangeUserRole;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class ChangeUserRoleCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly ChangeUserRoleCommandHandler _handler;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Role _marketing = new() { Id = Guid.NewGuid(), Name = "Marketing" };
    private readonly Role _procurement = new() { Id = Guid.NewGuid(), Name = "Procurement" };

    public ChangeUserRoleCommandHandlerTests()
    {
        _currentUserServiceMock.GetCurrentUserId().Returns(_adminId);
        _handler = new ChangeUserRoleCommandHandler(_usersMock, _unitOfWorkMock, _currentUserServiceMock);
    }

    private User MakeUserWithRole(Role role)
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Thabo", LastName = "Mokoena", Email = "thabo@saharvest.org", IsActive = true };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role, AssignedAt = DateTime.UtcNow.AddDays(-30) });
        return user;
    }

    // Returns the same tracked instance on every read; on re-read, populates the Role
    // navigation for whatever assignment the handler left behind (as EF would on reload).
    private void SetupRepository(User user)
    {
        var rolesById = new[] { _marketing, _procurement }.ToDictionary(r => r.Id);
        _usersMock.GetByIdWithRoleAsync(user.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            foreach (var ur in user.UserRoles)
                ur.Role = rolesById[ur.RoleId];
            return user;
        });
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _usersMock.GetByIdWithRoleAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new ChangeUserRoleCommand { Id = id, Request = new ChangeUserRoleRequest { RoleId = _procurement.Id } };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewRole_ReplacesAssignmentAndRecordsWhoAssignedIt()
    {
        var user = MakeUserWithRole(_marketing);
        SetupRepository(user);

        var command = new ChangeUserRoleCommand { Id = user.Id, Request = new ChangeUserRoleRequest { RoleId = _procurement.Id } };

        var result = await _handler.Handle(command, CancellationToken.None);

        var assignment = Assert.Single(user.UserRoles);
        Assert.Equal(_procurement.Id, assignment.RoleId);
        Assert.Equal(_adminId, assignment.AssignedByUserId);
        Assert.Equal(_procurement.Id, result.RoleId);
        Assert.Equal("Procurement", result.Role);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameRole_IsANoOpAndDoesNotSave()
    {
        // Re-adding a UserRole with an identical composite key would make EF throw on
        // SaveChanges — an unchanged submission must leave the assignment untouched.
        var user = MakeUserWithRole(_marketing);
        var originalAssignment = user.UserRoles.Single();
        SetupRepository(user);

        var command = new ChangeUserRoleCommand { Id = user.Id, Request = new ChangeUserRoleRequest { RoleId = _marketing.Id } };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Same(originalAssignment, Assert.Single(user.UserRoles));
        Assert.Equal("Marketing", result.Role);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PopulatesAuditOldAndNewValues()
    {
        var user = MakeUserWithRole(_marketing);
        SetupRepository(user);

        var command = new ChangeUserRoleCommand { Id = user.Id, Request = new ChangeUserRoleRequest { RoleId = _procurement.Id } };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(user.Id, command.EntityId);
        Assert.Contains(_marketing.Id.ToString(), command.OldValues!.ToString());
        Assert.Contains(_procurement.Id.ToString(), command.NewValues!.ToString());
    }
}
