namespace CRM.Application.Modules.Reports.Export;

public enum ReportExportFormat
{
    Pdf,
    Excel
}

/// <summary>A rendered report file, ready to upload.</summary>
/// <param name="FileExtension">Without the leading dot, e.g. <c>pdf</c> or <c>xlsx</c>.</param>
public sealed record ReportFile(byte[] Content, string ContentType, string FileExtension);
