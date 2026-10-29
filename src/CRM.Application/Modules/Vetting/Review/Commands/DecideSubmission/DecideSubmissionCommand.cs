namespace CRM.Application.Modules.Vetting.Review.Commands.DecideSubmission;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

// The audit row is the record of who decided; there is no reviewed_by column.
public class DecideSubmissionCommand : IRequest<VettingSubmissionDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Reason { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => string.Equals(Decision, nameof(VettingDecision.Approve), StringComparison.OrdinalIgnoreCase)
        ? AuditAction.Approved
        : AuditAction.Rejected;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
