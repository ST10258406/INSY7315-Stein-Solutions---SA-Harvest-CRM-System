namespace CRM.Application.Modules.Approvals.Commands.ApproveDonor;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

// Void command (204). IAuditableCommand → AuditBehaviour writes an "Approved" audit row.
public class ApproveDonorCommand : IRequest, IAuditableCommand
{
    public Guid ApprovalId { get; set; }

    public string EntityType => "DonorApproval";
    public AuditAction Action => AuditAction.Approved;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
