using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Heum.Server.Features.Tenants.Services;

public sealed class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    private const string ContainerName = "tenant-logos";
    private static readonly TimeSpan SasTtl = TimeSpan.FromMinutes(5);

    public async Task<(Uri UploadUrl, Uri BlobUrl)> GenerateLogoUploadUrlAsync(
        Guid tenantId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = tenantId.ToString();
        var blobClient = container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(SasTtl),
            ContentType = contentType,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var uploadUrl = blobClient.GenerateSasUri(sasBuilder);
        var blobUrl = new Uri($"{blobClient.Uri}");

        return (uploadUrl, blobUrl);
    }

    public async Task DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = container.GetBlobClient(tenantId.ToString());
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
