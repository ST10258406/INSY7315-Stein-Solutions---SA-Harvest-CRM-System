namespace CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using MediatR;

public class GetSiteVisitsQueryHandler : IRequestHandler<GetSiteVisitsQuery, List<VettingSiteVisitDto>>
{
    private readonly IVettingSiteVisitRepository _visits;

    public GetSiteVisitsQueryHandler(IVettingSiteVisitRepository visits) => _visits = visits;

    public Task<List<VettingSiteVisitDto>> Handle(GetSiteVisitsQuery request, CancellationToken cancellationToken)
        => _visits.GetByDateAsync(request.Date!.Value, request.OfficerId, cancellationToken);
}
