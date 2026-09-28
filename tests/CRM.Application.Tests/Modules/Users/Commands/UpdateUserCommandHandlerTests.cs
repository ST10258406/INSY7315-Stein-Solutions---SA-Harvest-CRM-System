using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.UpdateUser;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _handler = new UpdateUserCommandHandler(_usersMock, _unitOfWorkMock);
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
}
