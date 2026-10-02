namespace CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using MediatR;

public class GetDonorsContactedReportQueryHandler
    : IRequestHandler<GetDonorsContactedReportQuery, DonorsContactedReportDto>
{
    private readonly IReportsRepository _reports;

    public GetDonorsContactedReportQueryHandler(IReportsRepository reports) => _reports = reports;

    public async Task<DonorsContactedReportDto> Handle(
        GetDonorsContactedReportQuery request, CancellationToken cancellationToken)
    {
        // Validator has already guaranteed both are present and StartDate <= EndDate.
        var startDate = request.StartDate!.Value;
        var endDate = request.EndDate!.Value;

        // Inclusive of the whole requested day range. Kind must be UTC: the column is
        // `timestamptz` and Npgsql rejects an Unspecified-kind DateTime (which is what
        // DateOnly.ToDateTime(TimeOnly) produces by default).
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var (totalDonorsContacted, byManager) = await _reports.GetDonorsContactedAsync(
            startUtc, endUtc, request.RelationshipManagerId, cancellationToken);

        return new DonorsContactedReportDto
        {
            Period = new ReportPeriodDto { StartDate = startDate, EndDate = endDate },
            TotalDonorsContacted = totalDonorsContacted,
            ByManager = byManager
        };
    }
}
