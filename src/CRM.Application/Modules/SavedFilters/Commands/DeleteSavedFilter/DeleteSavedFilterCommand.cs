namespace CRM.Application.Modules.SavedFilters.Commands.DeleteSavedFilter;

using System.Text.Json.Serialization;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using MediatR;

/// <summary>DELETE /api/v1/saved-filters/{id}: soft-deletes one of the caller's saved filters.</summary>
public record DeleteSavedFilterCommand : IRequest, IAuditableCommand
{
    public Guid Id { get; init; }

    public string EntityType => "SavedFilter";
    public AuditAction Action => AuditAction.Deleted;

    [JsonIgnore] public Guid EntityId { get; set; }
    [JsonIgnore] public object? OldValues { get; set; }
    [JsonIgnore] public object? NewValues { get; set; }
}
