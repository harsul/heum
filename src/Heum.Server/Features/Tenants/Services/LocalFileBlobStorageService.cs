using Microsoft.AspNetCore.Hosting;

namespace Heum.Server.Features.Tenants.Services;

/// <summary>
/// Local-filesystem implementation of <see cref="IBlobStorageService"/>. Stores tenant logos
/// under <c>wwwroot/tenant-logos/{tenantId}/</c> and returns relative URL paths.
/// Select via configuration key <c>BlobStorage:Provider = "Local"</c>.
/// </summary>
internal sealed class LocalFileBlobStorageService(
    IWebHostEnvironment env,
    ILogger<LocalFileBlobStorageService> logger) : IBlobStorageService
{
    private const string FolderName = "tenant-logos";

    public async Task<Uri> UploadLogoAsync(
        Guid tenantId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, FolderName, tenantId.ToString());
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{ExtensionFor(contentType)}";
        var filePath = Path.Combine(directory, fileName);

        await using var file = File.Create(filePath);
        await content.CopyToAsync(file, cancellationToken);

        return new Uri($"/{FolderName}/{tenantId}/{fileName}", UriKind.Relative);
    }

    public Task DeleteLogoAsync(Uri logoUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var relativePath = logoUrl.IsAbsoluteUri ? logoUrl.AbsolutePath : logoUrl.OriginalString;
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete local logo file {LogoUrl}", logoUrl);
        }
        return Task.CompletedTask;
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        _ => string.Empty,
    };
}
