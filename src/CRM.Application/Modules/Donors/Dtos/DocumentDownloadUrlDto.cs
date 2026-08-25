namespace CRM.Application.Modules.Donors.Dtos;

public class DocumentDownloadUrlDto
{
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
}
