namespace CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using MediatR;

public class GetDonorsByRegionReportQueryHandler
    : IRequestHandler<GetDonorsByRegionReportQuery, List<DonorsByRegionDto>>
{
    private readonly IReportsRepository _reports;

    public GetDonorsByRegionReportQueryHandler(IReportsRepository reports) => _reports = reports;

    public Task<List<DonorsByRegionDto>> Handle(
        GetDonorsByRegionReportQuery request, CancellationToken cancellationToken)
        => _reports.GetDonorsByRegionAsync(cancellationToken);
}
