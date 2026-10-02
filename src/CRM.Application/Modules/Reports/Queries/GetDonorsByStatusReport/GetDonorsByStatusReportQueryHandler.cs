namespace CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using MediatR;

public class GetDonorsByStatusReportQueryHandler
    : IRequestHandler<GetDonorsByStatusReportQuery, List<DonorsByStatusDto>>
{
    private readonly IReportsRepository _reports;

    public GetDonorsByStatusReportQueryHandler(IReportsRepository reports) => _reports = reports;

    public Task<List<DonorsByStatusDto>> Handle(
        GetDonorsByStatusReportQuery request, CancellationToken cancellationToken)
        => _reports.GetDonorsByStatusAsync(cancellationToken);
}
