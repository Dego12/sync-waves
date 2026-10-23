using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SyncWaves.Auth.Domain.Abstractions;

namespace SyncWaves.Auth.Infrastructure.Implementations;

public class AzureBlobProfileImageStorage : IProfileImageStorage
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobProfileImageStorage> _logger;

    public AzureBlobProfileImageStorage(IConfiguration config, ILogger<AzureBlobProfileImageStorage> logger)
    {
        _logger = logger;
        _connectionString = config["AzureStorage:ConnectionString"]
                            ?? throw new InvalidOperationException("AzureStorage:ConnectionString not configured.");

        _containerName = config["AzureStorage:ProfileImagesContainer"]!;
    }

    public async Task<string> SaveProfileImageAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"{userId}/{Guid.NewGuid()}.png";

        var blobClient = containerClient.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(fileStream, headers, cancellationToken: cancellationToken);

        _logger.LogInformation("Uploaded profile image for user {UserId} to {BlobUri}", userId, blobClient.Uri);

        return blobClient.Uri.ToString();
    }
}
