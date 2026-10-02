namespace CRM.Application.Common.Interfaces;

using CRM.Application.Modules.Reports.Export;

/// <summary>
/// Renders an already-aggregated <see cref="ReportDocument"/> to a PDF or Excel file.
/// Rendering only — no data access and no upload — so the rendering libraries live entirely
/// in CRM.Infrastructure. Today the implementation runs synchronously inside the request
/// (fast enough at a single NGO's data volumes); the design doc's future Hangfire-generated
/// exports replace this implementation, not the export handler or controller.
/// </summary>
public interface IReportExportService
{
    Task<ReportFile> GenerateAsync(
        ReportDocument document, ReportExportFormat format, CancellationToken cancellationToken = default);
}
