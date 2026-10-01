namespace CRM.Infrastructure.Services;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using Microsoft.Extensions.Configuration;

public class BlobStorageService : IBlobStorageService
{
    private readonly string? _connectionString;
    private readonly string _containerName;
    private readonly Uri? _publicEndpoint;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["Azure:BlobStorage:ConnectionString"];
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "donor-documents";

        // Browser-reachable origin for download links. Locally the API talks to Azurite at the
        // docker-internal host "azurite", which a browser can't resolve; the SAS signature doesn't
        // cover the host, so the origin can be swapped without invalidating it.
        var publicEndpoint = configuration["Azure:BlobStorage:PublicEndpoint"];
        if (!string.IsNullOrWhiteSpace(publicEndpoint))
            _publicEndpoint = new Uri(publicEndpoint);
    }

    public async Task<BlobUploadResult> UploadAsync(Stream fileStream, string path, string contentType)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");

        var containerClient = new BlobContainerClient(_connectionString, _containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(path);
        await blobClient.UploadAsync(
            fileStream,
            new BlobHttpHeaders { ContentType = contentType });

        return new BlobUploadResult(path, blobClient.Uri.ToString());
    }

    public Task<string> GenerateSasUrlAsync(string path, TimeSpan expiry)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");

        var containerClient = new BlobContainerClient(_connectionString, _containerName);
        var blobClient = containerClient.GetBlobClient(path);

        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("Blob client cannot generate SAS URI — check storage account key auth is configured.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = path,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry),
            // Force a download rather than in-browser rendering of uploaded content.
            ContentDisposition = "attachment"
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        if (_publicEndpoint is not null)
        {
            sasUri = new UriBuilder(sasUri)
            {
                Scheme = _publicEndpoint.Scheme,
                Host = _publicEndpoint.Host,
                Port = _publicEndpoint.Port
            }.Uri;
        }

        return Task.FromResult(sasUri.ToString());
    }

    public Task DeleteAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");

        var containerClient = new BlobContainerClient(_connectionString, _containerName);
        return containerClient.GetBlobClient(path).DeleteIfExistsAsync();
    }
}
