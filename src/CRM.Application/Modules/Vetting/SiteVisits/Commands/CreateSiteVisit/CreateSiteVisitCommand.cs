namespace CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class CreateSiteVisitCommand : IRequest<VettingSiteVisitDto>, IAuditableCommand
{
    public Guid DonorId { get; set; }
    public Guid OfficerId { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    public string? LocationLabel { get; set; }
    public string? VisitNotes { get; set; }

    public string EntityType => "VettingSiteVisit";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
