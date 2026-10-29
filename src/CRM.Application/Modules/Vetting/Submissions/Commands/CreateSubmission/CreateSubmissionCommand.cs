namespace CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class CreateSubmissionCommand : IRequest<CreatedSubmissionDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public Guid OfficerId { get; set; }
    public Guid? SiteVisitId { get; set; }
    public VettingAssessmentDetailsDto? Details { get; set; }

    public string EntityType => "VettingSubmission";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
