namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAsync(Stream fileStream, string path, string contentType);
    Task<string> GenerateSasUrlAsync(string path, TimeSpan expiry);

    /// <summary>
    /// Permanently deletes the blob at the given path. This is a real, irreversible
    /// delete — there is no "soft delete" at the blob layer in this project. Donor
    /// document soft-delete is a DB-only concern (DonorDocument.IsActive); nothing
    /// in the donor document lifecycle calls this method.
    /// </summary>
    Task DeleteAsync(string path);
}
