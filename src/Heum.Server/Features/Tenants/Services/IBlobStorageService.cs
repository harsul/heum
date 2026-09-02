namespace Heum.Server.Features.Tenants.Services;

public interface IBlobStorageService
{
    Task<Uri> UploadLogoAsync(
        Guid tenantId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteLogoAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
