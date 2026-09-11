namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// Request body for POST /api/v1/reports/export. ReportType and Format are nullable so a
/// missing value reaches ExportReportCommandValidator (standard VALIDATION_ERROR envelope)
/// instead of being rejected earlier by MVC's implicit [Required] for non-nullable strings.
/// </summary>
public class ExportReportRequest
{
    public string? ReportType { get; set; }
    public string? Format { get; set; }
    public ExportReportFilters? Filters { get; set; }
}

/// <summary>
/// Only applies to the donors-contacted report (the only period-scoped report). For every
/// other report type these are ignored entirely — not validated, not applied.
/// </summary>
public class ExportReportFilters
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? RelationshipManagerId { get; set; }
}
