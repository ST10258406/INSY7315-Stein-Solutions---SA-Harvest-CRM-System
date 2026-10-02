namespace CRM.Infrastructure.Tests.Services;

using System.Text;
using ClosedXML.Excel;
using CRM.Application.Modules.Reports.Export;
using CRM.Infrastructure.Services;
using Xunit;

public class ReportExportServiceTests
{
    private readonly ReportExportService _service = new();

    private static ReportDocument Report(params object[][] rows) => new(
        Title: "Donors Contacted",
        GeneratedAtUtc: new DateTime(2026, 9, 8, 10, 30, 0, DateTimeKind.Utc),
        Metadata: [new("Period", "1 Jul 2026 – 31 Jul 2026"), new("Total donors contacted", "4")],
        Columns: [new("Relationship manager"), new("Donors contacted", IsNumeric: true), new("Total interactions", IsNumeric: true)],
        Rows: rows,
        Note: "A note under the table.");

    [Fact]
    public async Task GenerateAsync_Pdf_ReturnsPdfFile()
    {
        var file = await _service.GenerateAsync(Report(["Jane Doe", 3, 5]), ReportExportFormat.Pdf);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("pdf", file.FileExtension);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(file.Content, 0, 5));
    }

    [Fact]
    public async Task GenerateAsync_Excel_WritesMetadataHeaderAndNumericCells()
    {
        var file = await _service.GenerateAsync(
            Report(["Jane Doe", 3, 5], ["John Smith", 1, 1]), ReportExportFormat.Excel);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal("xlsx", file.FileExtension);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        var sheet = workbook.Worksheet("Donors Contacted");

        Assert.Equal("Donors Contacted", sheet.Cell(1, 1).GetString());
        Assert.Equal("Period", sheet.Cell(2, 1).GetString());
        Assert.Equal("1 Jul 2026 – 31 Jul 2026", sheet.Cell(2, 2).GetString());

        var header = sheet.CellsUsed().Single(c => c.GetString() == "Donors contacted");
        var firstDataRow = header.WorksheetRow().RowBelow();
        Assert.Equal("Jane Doe", firstDataRow.Cell(1).GetString());
        Assert.Equal(XLDataType.Number, firstDataRow.Cell(2).DataType);
        Assert.Equal(3, firstDataRow.Cell(2).GetDouble());
        Assert.Equal(5, firstDataRow.Cell(3).GetDouble());
        Assert.Equal("John Smith", firstDataRow.RowBelow().Cell(1).GetString());
    }

    [Theory]
    [InlineData(ReportExportFormat.Pdf)]
    [InlineData(ReportExportFormat.Excel)]
    public async Task GenerateAsync_NoRows_StillRendersAFile(ReportExportFormat format)
    {
        var file = await _service.GenerateAsync(Report(), format);

        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public async Task GenerateAsync_ExcelWithLongTitle_TruncatesSheetNameToExcelLimit()
    {
        var report = Report(["Jane Doe", 3, 5]) with { Title = "A report title that is far longer than Excel allows" };

        var file = await _service.GenerateAsync(report, ReportExportFormat.Excel);

        using var workbook = new XLWorkbook(new MemoryStream(file.Content));
        Assert.Equal(31, workbook.Worksheets.Single().Name.Length);
    }
}
