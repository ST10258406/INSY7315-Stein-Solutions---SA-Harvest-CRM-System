namespace CRM.Application.Modules.Reports.Export;

using System.Globalization;
using System.Text.RegularExpressions;
using CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// Maps each report's GET-endpoint DTO onto a <see cref="ReportDocument"/>. Presentation
/// only: every number comes straight from the DTO the GET endpoint already returns —
/// nothing is re-aggregated here, and nothing here touches the database.
/// </summary>
public static class ReportDocumentFactory
{
    public static ReportDocument DonorsContacted(
        DonorsContactedReportDto report, Guid? relationshipManagerId, DateTime generatedAtUtc)
    {
        var managerFilter = relationshipManagerId is null
            ? "All relationship managers"
            : report.ByManager.FirstOrDefault(m => m.Manager.Id == relationshipManagerId)?.Manager.FullName
              ?? relationshipManagerId.Value.ToString();

        return new ReportDocument(
            Title: "Donors Contacted",
            GeneratedAtUtc: generatedAtUtc,
            Metadata:
            [
                new("Period", $"{FormatDate(report.Period.StartDate)} – {FormatDate(report.Period.EndDate)}"),
                new("Relationship manager", managerFilter),
                new("Total donors contacted", FormatNumber(report.TotalDonorsContacted))
            ],
            Columns: [new("Relationship manager"), new("Donors contacted", IsNumeric: true), new("Total interactions", IsNumeric: true)],
            Rows: report.ByManager
                .Select(m => new object[] { m.Manager.FullName, m.DonorsContacted, m.TotalInteractions })
                .ToList(),
            Note: "Total donors contacted is a distinct count across the whole period and includes donors " +
                  "with no relationship manager assigned, so it can differ from the sum of the per-manager rows.");
    }

    public static ReportDocument DonorsByRegion(List<DonorsByRegionDto> rows, DateTime generatedAtUtc) => new(
        Title: "Donors by Region",
        GeneratedAtUtc: generatedAtUtc,
        Metadata: [Snapshot(generatedAtUtc)],
        Columns: [new("Region"), new("Code"), new("Donors", IsNumeric: true)],
        Rows: rows.Select(r => new object[] { r.RegionName, r.Region, r.DonorCount }).ToList(),
        Note: "A donor operating in more than one region is counted once per region, so these counts " +
              "can add up to more than the total number of donors.");

    public static ReportDocument DonorsByType(List<DonorsByTypeDto> rows, DateTime generatedAtUtc) => new(
        Title: "Donors by Donation Type",
        GeneratedAtUtc: generatedAtUtc,
        Metadata: [Snapshot(generatedAtUtc)],
        Columns: [new("Donation type"), new("Donors", IsNumeric: true)],
        Rows: rows.Select(r => new object[] { r.DonationType, r.DonorCount }).ToList(),
        Note: "A donor offering more than one donation type is counted once per type, so these counts " +
              "can add up to more than the total number of donors.");

    public static ReportDocument DonorsByStatus(List<DonorsByStatusDto> rows, DateTime generatedAtUtc) => new(
        Title: "Donors by Status",
        GeneratedAtUtc: generatedAtUtc,
        Metadata: [Snapshot(generatedAtUtc), new("Total donors", FormatNumber(rows.Sum(r => r.DonorCount)))],
        Columns: [new("Status"), new("Donors", IsNumeric: true)],
        Rows: rows.Select(r => new object[] { SplitPascalCase(r.Status), r.DonorCount }).ToList());

    private static ReportMetadataItem Snapshot(DateTime generatedAtUtc) =>
        new("Snapshot as at", generatedAtUtc.ToString("d MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture));

    private static string FormatDate(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    private static string FormatNumber(int value) => value.ToString(CultureInfo.InvariantCulture);

    // DonorStatus enum names ("PendingReview") → display text ("Pending Review").
    private static string SplitPascalCase(string value) => Regex.Replace(value, "(?<=[a-z])(?=[A-Z])", " ");
}
