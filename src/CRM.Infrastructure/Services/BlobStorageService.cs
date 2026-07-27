namespace CRM.Infrastructure.Services;

using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using Microsoft.Extensions.Configuration;

public class BlobStorageService : IBlobStorageService
{
    private readonly string? _connectionString;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["Azure:BlobStorage:ConnectionString"];
    }

    public Task<BlobUploadResult> UploadAsync(Stream fileStream, string path, string contentType)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");
            
        throw new NotImplementedException();
    }

    public Task<string> GenerateSasUrlAsync(string path, TimeSpan expiry)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");
            
        throw new NotImplementedException();
    }

    public Task SoftDeleteAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new NotImplementedException("Blob storage is not configured yet. Set Azure:BlobStorage:ConnectionString to enable this feature.");
            
        throw new NotImplementedException();
    }
}
