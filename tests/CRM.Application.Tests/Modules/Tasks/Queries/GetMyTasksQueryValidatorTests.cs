using CRM.Application.Modules.Tasks.Queries.GetMyTasks;

namespace CRM.Application.Tests.Modules.Tasks.Queries;

public class GetMyTasksQueryValidatorTests
{
    private readonly GetMyTasksQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Passes()
        => Assert.True(_validator.Validate(new GetMyTasksQuery()).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_PageBelowOne_Fails(int page)
        => Assert.False(_validator.Validate(new GetMyTasksQuery { Page = page }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(new GetMyTasksQuery { PageSize = pageSize }).IsValid);
}
