namespace CRM.Application.Modules.Vetting.Submissions.Commands.SubmitSubmission;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class SubmitSubmissionCommand : IRequest<VettingSubmissionDto>, IAuditableCommand
{
    public Guid Id { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
