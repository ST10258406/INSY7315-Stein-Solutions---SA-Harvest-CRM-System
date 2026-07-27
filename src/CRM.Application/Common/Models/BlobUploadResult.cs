namespace CRM.Application.Common.Models;

/// <summary>
/// Result of a blob upload. Path and BlobUrl are stored in donor_documents
/// (blob_storage_path, blob_storage_url respectively — see Section 3.6 of the
/// design doc). BlobUrl is the raw Azure Blob URL and must NEVER be returned to
/// the frontend directly — GenerateSasUrlAsync(Path, expiry) is what produces
/// the short-lived (15-min) URL that actually reaches the client.
/// </summary>
public record BlobUploadResult(string Path, string BlobUrl);
