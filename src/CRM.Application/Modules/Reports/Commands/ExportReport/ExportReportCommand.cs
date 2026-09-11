namespace CRM.Application.Modules.Reports.Commands.ExportReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Domain.Enums;
using MediatR;

// POST /api/v1/reports/export. A command rather than a query: it writes a new blob, and
// implementing IAuditableCommand makes AuditBehaviour write the "Exported" audit_logs row
// once the handler succeeds — the handler never touches audit_logs itself.
public class ExportReportCommand : IRequest<ReportExportDto>, IAuditableCommand
{
    public string? ReportType { get; set; }
    public string? Format { get; set; }

    /// <summary>Only read for donors-contacted; ignored for every other report type.</summary>
    public ExportReportFilters? Filters { get; set; }

    public string EntityType => "Report";
    public AuditAction Action => AuditAction.Exported;

    /// <summary>
    /// There's no report table to point at, so this is the id the handler generates for the
    /// export. It's also the blob folder name (reports/{reportType}/{EntityId}/{fileName}),
    /// so an audit row can be traced to the exact file in storage.
    /// </summary>
    public Guid EntityId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}
