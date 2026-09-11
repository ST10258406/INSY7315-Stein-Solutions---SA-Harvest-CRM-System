namespace CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;

using CRM.Application.Modules.Dashboard.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/dashboard/stats — aggregated KPIs for the dashboard home screen. Available to
/// any authenticated user; some fields are global and some are scoped to the caller (see
/// GetDashboardStatsQueryHandler for which is which).
/// </summary>
public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
