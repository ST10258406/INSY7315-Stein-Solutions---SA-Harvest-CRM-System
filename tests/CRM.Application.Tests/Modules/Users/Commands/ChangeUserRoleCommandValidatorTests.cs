using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.ChangeUserRole;
using CRM.Application.Modules.Users.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class ChangeUserRoleCommandValidatorTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly ChangeUserRoleCommandValidator _validator;
    private readonly Guid _roleId = Guid.NewGuid();

    public ChangeUserRoleCommandValidatorTests()
    {
        _usersMock.RoleExistsAsync(_roleId, Arg.Any<CancellationToken>()).Returns(true);
        _validator = new ChangeUserRoleCommandValidator(_usersMock);
    }

    private ChangeUserRoleCommand MakeValid() => new()
    {
        Id = Guid.NewGuid(),
        Request = new ChangeUserRoleRequest { RoleId = _roleId }
    };

    [Fact]
    public async Task Validate_ValidRequest_Passes()
        => Assert.True((await _validator.ValidateAsync(MakeValid())).IsValid);

    [Fact]
    public async Task Validate_EmptyId_Fails()
    {
        var c = MakeValid();
        c.Id = Guid.Empty;
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_NullRequest_Fails()
        => Assert.False((await _validator.ValidateAsync(new ChangeUserRoleCommand { Id = Guid.NewGuid(), Request = null! })).IsValid);

    [Fact]
    public async Task Validate_EmptyRoleId_Fails()
    {
        var c = MakeValid();
        c.Request.RoleId = Guid.Empty;
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_UnknownRoleId_Fails()
    {
        var c = MakeValid();
        c.Request.RoleId = Guid.NewGuid();

        var result = await _validator.ValidateAsync(c);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Invalid role.");
    }
}
