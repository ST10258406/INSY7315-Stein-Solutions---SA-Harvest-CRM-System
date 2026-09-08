using CRM.Application.Modules.Approvals.Queries.GetApprovals;

namespace CRM.Application.Tests.Modules.Approvals.Queries;

public class GetApprovalsQueryValidatorTests
{
    private readonly GetApprovalsQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Passes() => Assert.True(_validator.Validate(new GetApprovalsQuery()).IsValid);

    [Theory]
    [InlineData("Pending")]
    [InlineData("approved")]
    [InlineData("REJECTED")]
    public void Validate_KnownStatus_Passes(string status)
        => Assert.True(_validator.Validate(new GetApprovalsQuery { Status = status }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("all")]
    [InlineData("Waiting")]
    public void Validate_UnknownStatus_Fails(string status)
        => Assert.False(_validator.Validate(new GetApprovalsQuery { Status = status }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(new GetApprovalsQuery { PageSize = pageSize }).IsValid);
}
