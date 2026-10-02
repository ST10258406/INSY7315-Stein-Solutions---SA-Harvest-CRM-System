namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// Response for POST /api/v1/reports/export. DownloadUrl is a 15-minute read-only SAS URL —
/// never the raw blob URL.
/// </summary>
public class ReportExportDto
{
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
