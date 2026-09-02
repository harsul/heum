namespace Heum.Server.Features.Tenants.Services;

public interface IBlobStorageService
{
    Task<(Uri UploadUrl, Uri BlobUrl)> GenerateLogoUploadUrlAsync(
        Guid tenantId,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
