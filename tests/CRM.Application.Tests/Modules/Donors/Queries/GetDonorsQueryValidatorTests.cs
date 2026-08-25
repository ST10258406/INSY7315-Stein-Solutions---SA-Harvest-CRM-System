using CRM.Application.Modules.Donors.Queries.GetDonors;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDonorsQueryValidatorTests
{
    private readonly GetDonorsQueryValidator _validator = new();

    [Fact]
    public void ValidQuery_ShouldNotHaveAnyErrors()
    {
        var query = new GetDonorsQuery { Page = 1, PageSize = 20, SortDir = "asc" };

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PageLessThanOne_ShouldHaveValidationError()
    {
        var query = new GetDonorsQuery { Page = 0 };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSizeOutOfRange_ShouldHaveValidationError(int pageSize)
    {
        var query = new GetDonorsQuery { PageSize = pageSize };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void SortDirInvalid_ShouldHaveValidationError()
    {
        var query = new GetDonorsQuery { SortDir = "sideways" };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SortDir");
    }

    [Fact]
    public void SortByNotWhitelisted_ShouldHaveValidationError()
    {
        var query = new GetDonorsQuery { SortBy = "notAllowedField" };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SortBy");
    }

    [Theory]
    [InlineData("companyName")]
    [InlineData("COMPANYNAME")]
    [InlineData("companyname")]
    public void SortByWhitelisted_AnyCase_ShouldNotHaveValidationError(string sortBy)
    {
        var query = new GetDonorsQuery { SortBy = sortBy };

        var result = _validator.Validate(query);

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "SortBy");
    }

    [Fact]
    public void StatusInvalid_ShouldHaveValidationError()
    {
        var query = new GetDonorsQuery { Status = "NotARealStatus" };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
    }
}
