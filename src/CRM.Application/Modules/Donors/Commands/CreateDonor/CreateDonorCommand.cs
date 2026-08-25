namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Enums;
using MediatR;

// Implements IAuditableCommand so AuditBehaviour writes the audit_logs row —
// the handler must not touch AuditLogs directly (see AuditBehaviour.cs).
public class CreateDonorCommand : IRequest<DonorDetailDto>, IAuditableCommand
{
    public CreateDonorRequest Request { get; set; } = null!;

    public string EntityType => "Donor";
    public AuditAction Action => AuditAction.Created;
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
