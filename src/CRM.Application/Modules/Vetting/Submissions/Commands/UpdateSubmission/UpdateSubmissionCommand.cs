namespace CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UpdateSubmissionCommand : IRequest<VettingSubmissionDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public Guid? OfficerId { get; set; }
    public VettingAssessmentDetailsDto? Details { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
