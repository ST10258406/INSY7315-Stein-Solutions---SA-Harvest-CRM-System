namespace CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;

using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using MediatR;

/// <summary>GET /api/v1/vetting/site-visits?date=YYYY-MM-DD&amp;officerId= — one day's schedule.</summary>
public record GetSiteVisitsQuery : IRequest<List<VettingSiteVisitDto>>
{
    public DateOnly? Date { get; init; }
    public Guid? OfficerId { get; init; }
}
