using CRM.Application.Modules.Tasks.Queries.GetDonorTasks;

namespace CRM.Application.Tests.Modules.Tasks.Queries;

public class GetDonorTasksQueryValidatorTests
{
    private readonly GetDonorTasksQueryValidator _validator = new();

    private static GetDonorTasksQuery MakeValid() => new() { DonorId = Guid.NewGuid() };

    [Fact]
    public void Validate_Defaults_Passes() => Assert.True(_validator.Validate(MakeValid()).IsValid);

    [Fact]
    public void Validate_EmptyDonorId_Fails()
        => Assert.False(_validator.Validate(MakeValid() with { DonorId = Guid.Empty }).IsValid);

    [Theory]
    [InlineData("all")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("ALL")]
    [InlineData("True")]
    public void Validate_KnownIsCompletedValues_Pass(string value)
        => Assert.True(_validator.Validate(MakeValid() with { IsCompleted = value }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("yes")]
    [InlineData("1")]
    public void Validate_UnknownIsCompletedValues_Fail(string value)
        => Assert.False(_validator.Validate(MakeValid() with { IsCompleted = value }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(MakeValid() with { PageSize = pageSize }).IsValid);
}
