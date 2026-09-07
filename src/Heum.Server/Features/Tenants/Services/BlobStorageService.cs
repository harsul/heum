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
        // Logos are rendered straight from their URL by browsers, so the container stays publicly
        // readable. The blob name carries a random segment so URLs can't be enumerated by tenant id.
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"{tenantId}/{Guid.NewGuid():N}{ExtensionFor(contentType)}";
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);

        return blobClient.Uri;
    }

    public async Task DeleteLogoAsync(Uri logoUrl, CancellationToken cancellationToken = default)
    {
        var blobName = new BlobUriBuilder(logoUrl).BlobName;
        if (string.IsNullOrEmpty(blobName))
            return;

        var blobClient = blobServiceClient.GetBlobContainerClient(ContainerName).GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        _ => string.Empty,
    };
}
