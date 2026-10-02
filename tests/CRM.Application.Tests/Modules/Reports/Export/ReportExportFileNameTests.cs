using CRM.Application.Modules.Reports.Export;

namespace CRM.Application.Tests.Modules.Reports.Export;

public class ReportExportFileNameTests
{
    private static readonly DateOnly ExportDate = new(2026, 9, 8);

    [Theory]
    [InlineData("2026-07-01", "2026-07-31", "donors-contacted-july-2026.pdf")]
    [InlineData("2028-02-01", "2028-02-29", "donors-contacted-february-2028.pdf")]
    [InlineData("2026-07-01", "2026-07-15", "donors-contacted-2026-07-01-to-2026-07-15.pdf")]
    [InlineData("2026-07-02", "2026-07-31", "donors-contacted-2026-07-02-to-2026-07-31.pdf")]
    [InlineData("2026-06-01", "2026-07-31", "donors-contacted-2026-06-01-to-2026-07-31.pdf")]
    [InlineData("2026-07-15", "2026-07-15", "donors-contacted-2026-07-15.pdf")]
    public void Build_WithDateRange_DescribesThePeriod(string start, string end, string expected)
    {
        var fileName = ReportExportFileName.Build(
            "donors-contacted", DateOnly.Parse(start), DateOnly.Parse(end), ExportDate, "pdf");

        Assert.Equal(expected, fileName);
    }

    [Fact]
    public void Build_WithoutDateRange_UsesExportDate()
    {
        var fileName = ReportExportFileName.Build("donors-by-region", null, null, ExportDate, "xlsx");

        Assert.Equal("donors-by-region-2026-09-08.xlsx", fileName);
    }
}
