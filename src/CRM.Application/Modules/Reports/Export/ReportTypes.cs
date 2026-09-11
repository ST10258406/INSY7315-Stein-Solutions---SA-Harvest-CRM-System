namespace CRM.Application.Modules.Reports.Export;

/// <summary>
/// The report identifiers accepted by POST /api/v1/reports/export. Each one matches the
/// route segment of its GET /api/v1/reports/... endpoint, and is also used verbatim as the
/// file name prefix and the blob folder name.
/// </summary>
public static class ReportTypes
{
    public const string DonorsContacted = "donors-contacted";
    public const string DonorsByRegion = "donors-by-region";
    public const string DonorsByType = "donors-by-type";
    public const string DonorsByStatus = "donors-by-status";

    public static readonly IReadOnlyList<string> All =
        [DonorsContacted, DonorsByRegion, DonorsByType, DonorsByStatus];
}

/// <summary>The <c>format</c> values accepted by POST /api/v1/reports/export.</summary>
public static class ReportExportFormats
{
    public const string Pdf = "pdf";
    public const string Excel = "excel";

    public static readonly IReadOnlyList<string> All = [Pdf, Excel];
}
