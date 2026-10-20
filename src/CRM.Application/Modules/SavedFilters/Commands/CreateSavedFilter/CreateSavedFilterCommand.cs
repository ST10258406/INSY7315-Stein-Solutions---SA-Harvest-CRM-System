namespace CRM.Application.Modules.SavedFilters.Commands.CreateSavedFilter;

using System.Text.Json.Serialization;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.SavedFilters.Dtos;
using CRM.Domain.Enums;
using MediatR;

/// <summary>POST /api/v1/saved-filters: saves the caller's current donor-list filters under a name.</summary>
public record CreateSavedFilterCommand : IRequest<SavedFilterDto>, IAuditableCommand
{
    public string Name { get; init; } = string.Empty;
    public string Page { get; init; } = "Donors";
    public string QueryString { get; init; } = string.Empty;

    public string EntityType => "SavedFilter";
    public AuditAction Action => AuditAction.Created;

    // Set by the handler, never bound from the request body.
    [JsonIgnore] public Guid EntityId { get; set; }
    [JsonIgnore] public object? OldValues { get; set; }
    [JsonIgnore] public object? NewValues { get; set; }
}
