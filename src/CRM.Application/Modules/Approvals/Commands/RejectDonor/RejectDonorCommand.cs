namespace CRM.Application.Modules.Approvals.Commands.RejectDonor;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

// Void command (204). IAuditableCommand → AuditBehaviour writes a "Rejected" audit row.
public class RejectDonorCommand : IRequest, IAuditableCommand
{
    public Guid ApprovalId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;

    public string EntityType => "DonorApproval";
    public AuditAction Action => AuditAction.Rejected;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
