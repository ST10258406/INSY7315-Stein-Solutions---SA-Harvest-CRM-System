namespace CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;

using CRM.Application.Modules.Reports.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/reports/donors-by-type — current donor-count-by-donation-type snapshot for the
/// donation type coverage chart. No date filtering (per design doc — only Donors Contacted is
/// period-scoped) and no query params at all.
/// </summary>
public record GetDonorsByTypeReportQuery : IRequest<List<DonorsByTypeDto>>;
