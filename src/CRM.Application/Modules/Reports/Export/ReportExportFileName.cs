namespace CRM.Application.Modules.Reports.Export;

using System.Globalization;

/// <summary>
/// Human-readable export file names: the report type plus the period it covers, e.g.
/// <c>donors-contacted-july-2026.pdf</c>. Reports without a date range (the current-state
/// snapshots) use the export date instead, e.g. <c>donors-by-region-2026-09-08.xlsx</c>.
/// </summary>
public static class ReportExportFileName
{
    public static string Build(
        string reportType, DateOnly? startDate, DateOnly? endDate, DateOnly exportDate, string fileExtension)
    {
        var period = startDate is { } start && endDate is { } end
            ? DescribePeriod(start, end)
            : Iso(exportDate);

        return $"{reportType}-{period}.{fileExtension}";
    }

    private static string DescribePeriod(DateOnly start, DateOnly end)
    {
        if (start == end)
            return Iso(start);

        var isWholeCalendarMonth = start.Day == 1
            && end.Year == start.Year
            && end.Month == start.Month
            && end.Day == DateTime.DaysInMonth(end.Year, end.Month);

        return isWholeCalendarMonth
            ? $"{start.ToString("MMMM", CultureInfo.InvariantCulture).ToLowerInvariant()}-{start.Year}"
            : $"{Iso(start)}-to-{Iso(end)}";
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
