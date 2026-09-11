namespace CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;

using CRM.Application.Modules.Reports.Dtos;
using MediatR;

/// <summary>
/// GET /api/v1/reports/donors-contacted — the flagship monthly KPI: how many donors were
/// contacted in a period, broken down by relationship manager.
/// </summary>
public record GetDonorsContactedReportQuery : IRequest<DonorsContactedReportDto>
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? RelationshipManagerId { get; init; }
}
