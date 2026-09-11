namespace CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;

using CRM.Application.Modules.Reports.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/reports/donors-by-status — current donor-count-by-status snapshot for the
/// status breakdown pie chart. No date filtering (per design doc — only Donors Contacted is
/// period-scoped) and no query params at all.
/// </summary>
public record GetDonorsByStatusReportQuery : IRequest<List<DonorsByStatusDto>>;
