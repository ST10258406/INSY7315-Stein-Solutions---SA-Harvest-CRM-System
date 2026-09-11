namespace CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using MediatR;

public class GetDonorsByTypeReportQueryHandler
    : IRequestHandler<GetDonorsByTypeReportQuery, List<DonorsByTypeDto>>
{
    private readonly IReportsRepository _reports;

    public GetDonorsByTypeReportQueryHandler(IReportsRepository reports) => _reports = reports;

    public Task<List<DonorsByTypeDto>> Handle(
        GetDonorsByTypeReportQuery request, CancellationToken cancellationToken)
        => _reports.GetDonorsByTypeAsync(cancellationToken);
}
