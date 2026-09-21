using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.CreateUser;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_usersMock, _unitOfWorkMock, _currentUserServiceMock);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesUserWithHashedTemporaryPassword()
    {
        var adminId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Marketing" };
        _currentUserServiceMock.GetCurrentUserId().Returns(adminId);

        User? added = null;
        _usersMock.AddAsync(Arg.Do<User>(u => added = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _usersMock.GetByIdWithRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // Simulates re-reading from the DB with the Role navigation populated —
                // the handler already added a UserRole (RoleId only, no Role loaded) to
                // `added` before this call, so just populate that same row's Role rather
                // than appending a second one.
                added!.UserRoles.Single().Role = role;
                return added;
            });

        var command = new CreateUserCommand
        {
            Request = new CreateUserRequest { FirstName = "Ayesha", LastName = "Cassim", Email = "ayesha@saharvest.org", RoleId = roleId }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(added);
        Assert.False(string.IsNullOrWhiteSpace(added!.PasswordHash));
        // The plaintext temp password must never end up stored verbatim — only its hash.
        Assert.NotEqual(result.TemporaryPassword, added.PasswordHash);

        var hasher = new PasswordHasher<User>();
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(added, added.PasswordHash, result.TemporaryPassword));

        Assert.Equal("ayesha@saharvest.org", result.Email);
        Assert.Equal("Marketing", result.Role);
        Assert.True(result.IsActive);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NeverIncludesPlaintextPasswordInAuditNewValues()
    {
        var adminId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var role = new Role { Id = roleId, Name = "Admin" };
        _currentUserServiceMock.GetCurrentUserId().Returns(adminId);

        User? added = null;
        _usersMock.AddAsync(Arg.Do<User>(u => added = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _usersMock.GetByIdWithRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                added!.UserRoles.Single().Role = role;
                return added;
            });

        var command = new CreateUserCommand
        {
            Request = new CreateUserRequest { FirstName = "Nomsa", LastName = "Dube", Email = "nomsa@saharvest.org", RoleId = roleId }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.DoesNotContain(result.TemporaryPassword, command.NewValues!.ToString());
    }
}
