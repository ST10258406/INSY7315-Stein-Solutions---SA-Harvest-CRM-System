using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;

namespace CRM.Application.Tests.Modules.Interactions.Queries;

public class GetDonorInteractionsQueryValidatorTests
{
    private readonly GetDonorInteractionsQueryValidator _validator = new();

    private static GetDonorInteractionsQuery MakeValid() => new()
    {
        DonorId = Guid.NewGuid(),
        Page = 1,
        PageSize = 20,
        InteractionType = null
    };

    [Fact]
    public void Validate_Defaults_Passes() => Assert.True(_validator.Validate(MakeValid()).IsValid);

    [Fact]
    public void Validate_EmptyDonorId_Fails()
        => Assert.False(_validator.Validate(MakeValid() with { DonorId = Guid.Empty }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_Fails(int page)
        => Assert.False(_validator.Validate(MakeValid() with { Page = page }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(MakeValid() with { PageSize = pageSize }).IsValid);

    [Theory]
    [InlineData("Call")]
    [InlineData("email")]
    [InlineData(null)]
    public void Validate_KnownOrAbsentInteractionType_Passes(string? type)
        => Assert.True(_validator.Validate(MakeValid() with { InteractionType = type }).IsValid);

    [Fact]
    public void Validate_UnknownInteractionType_Fails()
        => Assert.False(_validator.Validate(MakeValid() with { InteractionType = "Smoke signal" }).IsValid);
}
