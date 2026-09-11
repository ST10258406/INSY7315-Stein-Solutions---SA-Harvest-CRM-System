namespace CRM.Application.Modules.Reports.Export;

/// <summary>
/// Format-neutral description of an exported report: a title, a few label/value lines
/// describing the scope (period, filters, totals), and one table. Built by
/// <see cref="ReportDocumentFactory"/> from a GET endpoint's DTO and rendered to PDF or
/// Excel by <see cref="Common.Interfaces.IReportExportService"/> — so the renderer never
/// needs to know which report it is drawing.
/// </summary>
/// <param name="Rows">One entry per table row, in the same order as <paramref name="Columns"/>.
/// Cells are <see cref="string"/> or a number (written to Excel as a numeric cell).</param>
public sealed record ReportDocument(
    string Title,
    DateTime GeneratedAtUtc,
    IReadOnlyList<ReportMetadataItem> Metadata,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyList<object>> Rows,
    string? Note = null);

public sealed record ReportMetadataItem(string Label, string Value);

public sealed record ReportColumn(string Header, bool IsNumeric = false);
