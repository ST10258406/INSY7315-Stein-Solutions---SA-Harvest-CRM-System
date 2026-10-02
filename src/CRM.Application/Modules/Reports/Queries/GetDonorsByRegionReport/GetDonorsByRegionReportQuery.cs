namespace CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;

using CRM.Application.Modules.Reports.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/reports/donors-by-region — current donor-count-by-region snapshot for the
/// regional coverage chart. No date filtering (per design doc — only Donors Contacted is
/// period-scoped) and no query params at all.
/// </summary>
public record GetDonorsByRegionReportQuery : IRequest<List<DonorsByRegionDto>>;
