using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Users.Commands.UpdateUser;
using CRM.Application.Modules.Users.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Commands;

public class UpdateUserCommandValidatorTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly UpdateUserCommandValidator _validator;

    public UpdateUserCommandValidatorTests()
    {
        _usersMock.EmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _validator = new UpdateUserCommandValidator(_usersMock);
    }

    private static UpdateUserCommand MakeValid() => new()
    {
        Id = Guid.NewGuid(),
        Request = new UpdateUserRequest { FirstName = "Johan", LastName = "Pretorius", Email = "johan@saharvest.org" }
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
        => Assert.False((await _validator.ValidateAsync(new UpdateUserCommand { Id = Guid.NewGuid(), Request = null! })).IsValid);

    [Fact]
    public async Task Validate_BlankFirstName_Fails()
    {
        var c = MakeValid();
        c.Request.FirstName = "";
        Assert.False((await _validator.ValidateAsync(c)).IsValid);
    }

    [Fact]
    public async Task Validate_LastNameTooLong_Fails()
    {
        var c = MakeValid();
        c.Request.LastName = new string('a', 101);
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
    public async Task Validate_EmailTakenByAnotherUser_Fails()
    {
        var c = MakeValid();
        _usersMock.EmailExistsAsync(c.Request.Email, c.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _validator.ValidateAsync(c);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A user with this email already exists.");
    }

    [Fact]
    public async Task Validate_UniquenessCheck_ExcludesTheUserBeingEdited()
    {
        // Keeping your own email unchanged must not count as a duplicate of yourself.
        var c = MakeValid();

        await _validator.ValidateAsync(c);

        await _usersMock.Received(1).EmailExistsAsync(c.Request.Email, c.Id, Arg.Any<CancellationToken>());
    }
}
