using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Heum.Server.Features.Tenants.Services;

public sealed class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    private const string ContainerName = "tenant-logos";

    public async Task<Uri> UploadLogoAsync(
        Guid tenantId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = container.GetBlobClient(tenantId.ToString());
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);

        return blobClient.Uri;
    }

    public async Task DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = container.GetBlobClient(tenantId.ToString());
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
