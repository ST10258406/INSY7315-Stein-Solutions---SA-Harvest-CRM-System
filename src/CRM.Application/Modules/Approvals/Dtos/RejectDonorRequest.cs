namespace CRM.Application.Modules.Approvals.Dtos;

/// <summary>Request body for POST /api/v1/approvals/{id}/reject.</summary>
public class RejectDonorRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}
