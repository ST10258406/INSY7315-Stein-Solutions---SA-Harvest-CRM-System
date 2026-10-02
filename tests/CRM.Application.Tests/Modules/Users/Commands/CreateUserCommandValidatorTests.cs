using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.CreateUser;
using CRM.Application.Modules.Users.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly CreateUserCommandValidator _validator;
    private readonly Guid _roleId = Guid.NewGuid();

    public CreateUserCommandValidatorTests()
    {
        _usersMock.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersMock.RoleExistsAsync(_roleId, Arg.Any<CancellationToken>()).Returns(true);
        _validator = new CreateUserCommandValidator(_usersMock);
    }

    private CreateUserCommand MakeValid() => new()
    {
        Request = new CreateUserRequest { FirstName = "Ayesha", LastName = "Cassim", Email = "ayesha@saharvest.org", RoleId = _roleId }
    };

    [Fact]
    public async Task Validate_ValidRequest_Passes()
        => Assert.True((await _validator.ValidateAsync(MakeValid())).IsValid);

    [Fact]
    public async Task Validate_NullRequest_Fails()
        => Assert.False((await _validator.ValidateAsync(new CreateUserCommand { Request = null! })).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_BlankFirstName_Fails(string firstName)
    {
        var c = MakeValid();
        c.Request.FirstName = firstName;
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_FirstNameTooLong_Fails()
    {
        var c = MakeValid();
        c.Request.FirstName = new string('a', 101);
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_BlankLastName_Fails()
    {
        var c = MakeValid();
        c.Request.LastName = "";
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_InvalidEmail_Fails(string email)
    {
        var c = MakeValid();
        c.Request.Email = email;
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_DuplicateEmail_Fails()
    {
        _usersMock.EmailExistsAsync("ayesha@saharvest.org", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _validator.ValidateAsync(MakeValid());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A user with this email already exists.");
    }

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

    [Fact]
    public async Task Validate_ReservedSystemDomain_Fails()
    {
        var c = MakeValid();
        c.Request.Email = "impostor@system.local";

        var result = await _validator.ValidateAsync(c);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "This email domain is reserved.");
    }
}
