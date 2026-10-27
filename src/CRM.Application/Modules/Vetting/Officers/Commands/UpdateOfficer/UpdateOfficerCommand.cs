namespace CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class UpdateOfficerCommand : IRequest<VettingOfficerDto>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Initials { get; set; }
    public string? AssignedRegion { get; set; }

    public string EntityType => "VettingOfficer";
    public AuditAction Action => AuditAction.Updated;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
