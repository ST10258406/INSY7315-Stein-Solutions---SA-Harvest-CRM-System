using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;

namespace CRM.Application.Tests.Modules.Reports.Queries;

public class GetDonorsContactedReportQueryValidatorTests
{
    private readonly GetDonorsContactedReportQueryValidator _validator = new();

    [Fact]
    public void Validate_MissingStartDate_Fails()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = null,
            EndDate = new DateOnly(2026, 7, 31)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetDonorsContactedReportQuery.StartDate));
    }

    [Fact]
    public void Validate_MissingEndDate_Fails()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = null
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetDonorsContactedReportQuery.EndDate));
    }

    [Fact]
    public void Validate_BothDatesMissing_FailsWithTwoErrors()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery());

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_StartDateAfterEndDate_Fails()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 7, 1)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_StartDateEqualsEndDate_Passes()
    {
        var date = new DateOnly(2026, 7, 15);

        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = date,
            EndDate = date
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRange_Passes()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRangeWithManagerFilter_Passes()
    {
        var result = _validator.Validate(new GetDonorsContactedReportQuery
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            RelationshipManagerId = Guid.NewGuid()
        });

        Assert.True(result.IsValid);
    }
}
