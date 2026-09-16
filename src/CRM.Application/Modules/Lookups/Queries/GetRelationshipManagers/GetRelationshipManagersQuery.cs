namespace CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;

using CRM.Application.Modules.Donors.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/lookups/relationship-managers — active users holding the Procurement role
/// (the CRM's relationship managers), for the reports "Relationship manager" filter.
/// </summary>
public record GetRelationshipManagersQuery : IRequest<List<RelationshipManagerDto>>;
