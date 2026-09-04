namespace Heum.Server.Features.Tenants.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a tenant logo under a fresh, unguessable blob name and returns its public URL.
    /// The previous logo (if any) is not touched - callers delete it via
    /// <see cref="DeleteLogoAsync"/> once the new URL has been persisted.
    /// </summary>
    Task<Uri> UploadLogoAsync(
        Guid tenantId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the blob a previously returned logo URL points to. No-op if it's gone already.</summary>
    Task DeleteLogoAsync(Uri logoUrl, CancellationToken cancellationToken = default);
}
