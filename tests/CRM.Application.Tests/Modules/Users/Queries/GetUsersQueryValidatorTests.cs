using CRM.Application.Modules.Users.Queries.GetUsers;

namespace CRM.Application.Tests.Modules.Users.Queries;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Pass()
        => Assert.True(_validator.Validate(new GetUsersQuery()).IsValid);

    [Fact]
    public void Validate_PageZero_Fails()
        => Assert.False(_validator.Validate(new GetUsersQuery { Page = 0 }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(new GetUsersQuery { PageSize = pageSize }).IsValid);

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_PageSizeAtBounds_Passes(int pageSize)
        => Assert.True(_validator.Validate(new GetUsersQuery { PageSize = pageSize }).IsValid);

    [Theory]
    [InlineData("asc")]
    [InlineData("DESC")]
    public void Validate_KnownSortDir_Passes(string sortDir)
        => Assert.True(_validator.Validate(new GetUsersQuery { SortDir = sortDir }).IsValid);

    [Fact]
    public void Validate_UnknownSortDir_Fails()
        => Assert.False(_validator.Validate(new GetUsersQuery { SortDir = "sideways" }).IsValid);

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("createdAt")]
    [InlineData("CREATEDAT")]
    public void Validate_WhitelistedSortBy_Passes(string sortBy)
        => Assert.True(_validator.Validate(new GetUsersQuery { SortBy = sortBy }).IsValid);

    [Theory]
    [InlineData("passwordHash")]
    [InlineData("isActive")]
    public void Validate_NonWhitelistedSortBy_Fails(string sortBy)
        => Assert.False(_validator.Validate(new GetUsersQuery { SortBy = sortBy }).IsValid);
}
