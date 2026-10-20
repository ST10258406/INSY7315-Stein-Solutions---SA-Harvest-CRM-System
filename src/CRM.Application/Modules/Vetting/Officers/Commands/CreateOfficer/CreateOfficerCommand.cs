namespace CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Enums;
using MediatR;

public class CreateOfficerCommand : IRequest<VettingOfficerDto>, IAuditableCommand
{
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AssignedRegion { get; set; } = string.Empty;

    public string EntityType => "VettingOfficer";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
