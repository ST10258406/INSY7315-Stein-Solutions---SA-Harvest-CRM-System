using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.CreateUser;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly IUserPasswordHasher _passwordHasherMock = Substitute.For<IUserPasswordHasher>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_usersMock, _unitOfWorkMock, _currentUserServiceMock, _passwordHasherMock);
        _passwordHasherMock.HashPassword(Arg.Any<string>()).Returns(ci => $"hashed:{ci.Arg<string>()}");
    }

    private static CreateUserCommand MakeCommand(Guid roleId, string email = "ayesha@saharvest.org") => new()
    {
        Request = new CreateUserRequest { FirstName = "Ayesha", LastName = "Cassim", Email = email, RoleId = roleId }
    };

    [Fact]
    public async Task Handle_ValidRequest_CreatesUserWithHashedTemporaryPassword()
    {
        var adminId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _currentUserServiceMock.GetCurrentUserId().Returns(adminId);
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Marketing");

        User? added = null;
        _usersMock.AddAsync(Arg.Do<User>(u => added = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(MakeCommand(roleId), CancellationToken.None);

        Assert.NotNull(added);
        Assert.False(string.IsNullOrWhiteSpace(added!.PasswordHash));
        // The plaintext temp password must never end up stored verbatim — only its hash,
        // produced through the Application-owned IUserPasswordHasher abstraction (not a
        // direct Microsoft.AspNetCore.Identity dependency in this layer).
        Assert.NotEqual(result.TemporaryPassword, added.PasswordHash);
        Assert.Equal($"hashed:{result.TemporaryPassword}", added.PasswordHash);

        var userRole = Assert.Single(added.UserRoles);
        Assert.Equal(roleId, userRole.RoleId);
        Assert.Equal(adminId, userRole.AssignedByUserId);

        Assert.Equal(added.Id, result.Id);
        Assert.Equal("ayesha@saharvest.org", result.Email);
        Assert.Equal(roleId, result.RoleId);
        Assert.Equal("Marketing", result.Role);
        Assert.True(result.IsActive);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewUser_MustChangeTheTemporaryPassword()
    {
        var roleId = Guid.NewGuid();
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Marketing");
        User? added = null;
        _usersMock.AddAsync(Arg.Do<User>(u => added = u), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _handler.Handle(MakeCommand(roleId), CancellationToken.None);

        Assert.True(added!.MustChangePassword);
    }

    [Fact]
    public async Task Handle_DoesNotReReadTheUserAfterCommitting()
    {
        // Regression guard: any failure after SaveChanges would lose the one-time
        // temporary password for an account that already exists. The response must be
        // built from data resolved before the commit, so no post-commit read may happen.
        var roleId = Guid.NewGuid();
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Admin");
        _usersMock.GetByIdWithRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Transient DB failure"));

        var result = await _handler.Handle(MakeCommand(roleId), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.TemporaryPassword));
        await _usersMock.DidNotReceive().GetByIdWithRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResolvesRoleNameBeforeCommitting()
    {
        var roleId = Guid.NewGuid();
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Procurement");

        await _handler.Handle(MakeCommand(roleId), CancellationToken.None);

        Received.InOrder(() =>
        {
            _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>());
            _unitOfWorkMock.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_SetsAuditEntityIdToTheNewUser()
    {
        var roleId = Guid.NewGuid();
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Admin");

        var command = MakeCommand(roleId);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(result.Id, command.EntityId);
    }

    [Fact]
    public async Task Handle_NeverIncludesPlaintextPasswordInAuditNewValues()
    {
        var roleId = Guid.NewGuid();
        _usersMock.GetRoleNameAsync(roleId, Arg.Any<CancellationToken>()).Returns("Admin");

        var command = MakeCommand(roleId, "nomsa@saharvest.org");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.DoesNotContain(result.TemporaryPassword, command.NewValues!.ToString());
    }
}
