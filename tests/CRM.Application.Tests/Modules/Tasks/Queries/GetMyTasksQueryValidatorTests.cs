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

    [Fact]
    public void Validate_DueAfterLaterThanDueBefore_Fails()
        => Assert.False(_validator.Validate(new GetMyTasksQuery
        {
            DueAfter = new DateOnly(2026, 10, 31),
            DueBefore = new DateOnly(2026, 10, 1)
        }).IsValid);

    [Theory]
    [InlineData(62, true)]
    [InlineData(63, false)]
    public void Validate_RangeIsCappedAt62Days(int days, bool valid)
    {
        var from = new DateOnly(2026, 1, 1);
        Assert.Equal(valid, _validator.Validate(new GetMyTasksQuery { DueAfter = from, DueBefore = from.AddDays(days) }).IsValid);
    }

    [Fact]
    public void Validate_OnlyOneBound_Passes()
        => Assert.True(_validator.Validate(new GetMyTasksQuery { DueAfter = new DateOnly(2020, 1, 1) }).IsValid);
}
