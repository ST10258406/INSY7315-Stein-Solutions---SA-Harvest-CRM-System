namespace CRM.Infrastructure.Services;

using System.Globalization;
using ClosedXML.Excel;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

/// <summary>
/// Synchronous, in-request report rendering: QuestPDF for PDF, ClosedXML for Excel. Knows
/// nothing about individual reports — it draws whatever <see cref="ReportDocument"/> it's given.
/// </summary>
public class ReportExportService : IReportExportService
{
    private const string PdfContentType = "application/pdf";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int ExcelMaxSheetNameLength = 31;

    static ReportExportService()
    {
        // QuestPDF Community licence: free for non-profits and for organisations under
        // USD 1M annual gross revenue. SA Harvest is a registered NPO — see issue #66 for the
        // licensing note, and revisit if this codebase is ever reused commercially.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<ReportFile> GenerateAsync(
        ReportDocument document, ReportExportFormat format, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var file = format switch
        {
            ReportExportFormat.Pdf => new ReportFile(RenderPdf(document), PdfContentType, "pdf"),
            ReportExportFormat.Excel => new ReportFile(RenderExcel(document), ExcelContentType, "xlsx"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report export format.")
        };

        return Task.FromResult(file);
    }

    private static byte[] RenderPdf(ReportDocument report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Text("SA Harvest CRM").FontSize(9).FontColor(Colors.Grey.Darken1);
                    header.Item().Text(report.Title).FontSize(18).SemiBold();
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    content.Spacing(12);

                    if (report.Metadata.Count > 0)
                    {
                        content.Item().Table(metadata =>
                        {
                            metadata.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(160);
                                columns.RelativeColumn();
                            });

                            foreach (var item in report.Metadata)
                            {
                                metadata.Cell().PaddingVertical(2).Text(item.Label).SemiBold();
                                metadata.Cell().PaddingVertical(2).Text(item.Value);
                            }
                        });
                    }

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var column in report.Columns)
                            {
                                if (column.IsNumeric)
                                    columns.ConstantColumn(110);
                                else
                                    columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var column in report.Columns)
                            {
                                var cell = header.Cell().Background(Colors.Grey.Lighten3).Padding(5);
                                (column.IsNumeric ? cell.AlignRight() : cell).Text(column.Header).SemiBold();
                            }
                        });

                        foreach (var row in report.Rows)
                        {
                            for (var i = 0; i < report.Columns.Count; i++)
                            {
                                var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);
                                (report.Columns[i].IsNumeric ? cell.AlignRight() : cell).Text(FormatCell(row[i]));
                            }
                        }
                    });

                    if (report.Rows.Count == 0)
                        content.Item().Text("No data for this report.").Italic().FontColor(Colors.Grey.Darken1);

                    if (report.Note is not null)
                        content.Item().Text(report.Note).FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem()
                        .Text($"Generated {report.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);

                    footer.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Darken1));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static byte[] RenderExcel(ReportDocument report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(
            report.Title.Length > ExcelMaxSheetNameLength ? report.Title[..ExcelMaxSheetNameLength] : report.Title);

        var title = sheet.Cell(1, 1);
        title.Value = report.Title;
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;

        var rowNumber = 2;
        foreach (var item in report.Metadata)
        {
            sheet.Cell(rowNumber, 1).Value = item.Label;
            sheet.Cell(rowNumber, 1).Style.Font.Bold = true;
            sheet.Cell(rowNumber, 2).Value = item.Value;
            rowNumber++;
        }

        var generated = sheet.Cell(rowNumber, 2);
        sheet.Cell(rowNumber, 1).Value = "Generated (UTC)";
        sheet.Cell(rowNumber, 1).Style.Font.Bold = true;
        generated.Value = report.GeneratedAtUtc;
        generated.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        generated.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        // Blank spacer row, then the table.
        var headerRow = rowNumber + 2;
        for (var i = 0; i < report.Columns.Count; i++)
        {
            var header = sheet.Cell(headerRow, i + 1);
            header.Value = report.Columns[i].Header;
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE");
        }

        rowNumber = headerRow;
        foreach (var row in report.Rows)
        {
            rowNumber++;
            for (var i = 0; i < report.Columns.Count; i++)
                SetCellValue(sheet.Cell(rowNumber, i + 1), row[i]);
        }

        // Size columns to the metadata + table only — the long title and note would
        // otherwise stretch column A; they simply overflow into the empty cells instead.
        sheet.Columns(1, Math.Max(report.Columns.Count, 2)).AdjustToContents(2, rowNumber);

        if (report.Note is not null)
        {
            var note = sheet.Cell(rowNumber + 2, 1);
            note.Value = report.Note;
            note.Style.Font.Italic = true;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value is null)
            cell.Value = Blank.Value;
        else if (value is string text)
            cell.Value = text;
        else
            cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static string FormatCell(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
