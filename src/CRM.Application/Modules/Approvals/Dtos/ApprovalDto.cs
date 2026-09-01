namespace CRM.Application.Modules.Approvals.Dtos;

/// <summary>One row of the donor approval queue, with the donor and the submitter nested.</summary>
public class ApprovalDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApprovalDonorDto Donor { get; set; } = null!;
    public ApprovalUserDto RequestedBy { get; set; } = null!;
    public ApprovalUserDto? ReviewedBy { get; set; }
}

public class ApprovalDonorDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ApprovalUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
