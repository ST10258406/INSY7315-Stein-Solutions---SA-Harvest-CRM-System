using CRM.Application.Modules.Approvals.Commands.RejectDonor;

namespace CRM.Application.Tests.Modules.Approvals.Commands;

public class RejectDonorCommandValidatorTests
{
    private readonly RejectDonorCommandValidator _validator = new();

    private static RejectDonorCommand MakeValid() => new()
    {
        ApprovalId = Guid.NewGuid(),
        RejectionReason = "Does not meet food-safety requirements."
    };

    [Fact]
    public void Validate_ValidCommand_Passes() => Assert.True(_validator.Validate(MakeValid()).IsValid);

    [Fact]
    public void Validate_EmptyApprovalId_Fails()
    {
        var c = MakeValid();
        c.ApprovalId = Guid.Empty;
        Assert.False(_validator.Validate(c).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingReason_Fails(string reason)
    {
        var c = MakeValid();
        c.RejectionReason = reason;
        Assert.False(_validator.Validate(c).IsValid);
    }
}
