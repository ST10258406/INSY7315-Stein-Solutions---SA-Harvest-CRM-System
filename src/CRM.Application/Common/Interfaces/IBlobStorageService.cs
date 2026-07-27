namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAsync(Stream fileStream, string path, string contentType);
    Task<string> GenerateSasUrlAsync(string path, TimeSpan expiry);
    Task SoftDeleteAsync(string path);
}
