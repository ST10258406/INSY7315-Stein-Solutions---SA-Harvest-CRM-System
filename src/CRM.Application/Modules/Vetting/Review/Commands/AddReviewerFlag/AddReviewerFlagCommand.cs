namespace CRM.Application.Modules.Vetting.Review.Commands.AddReviewerFlag;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class AddReviewerFlagCommand : IRequest<VettingFlagDto>, IAuditableCommand
{
    public Guid SubmissionId { get; set; }
    public string Description { get; set; } = string.Empty;

    public string EntityType => "VettingFlag";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
