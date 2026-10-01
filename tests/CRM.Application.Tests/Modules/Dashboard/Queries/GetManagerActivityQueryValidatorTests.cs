using CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;

namespace CRM.Application.Tests.Modules.Dashboard.Queries;

public class GetManagerActivityQueryValidatorTests
{
    private readonly GetManagerActivityQueryValidator _validator = new();

    [Theory]
    [InlineData("weekly")]
    [InlineData("monthly")]
    [InlineData("WEEKLY")]
    public void Validate_AcceptsKnownPeriods(string period)
        => Assert.True(_validator.Validate(new GetManagerActivityQuery(period)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("daily")]
    [InlineData("yearly")]
    public void Validate_RejectsUnknownPeriods(string period)
        => Assert.False(_validator.Validate(new GetManagerActivityQuery(period)).IsValid);
}
