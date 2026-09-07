using Heum.Server.Features.Tenants.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Heum.Server.xUnit;

public sealed class LocalFileBlobStorageServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LocalFileBlobStorageService _sut;

    public LocalFileBlobStorageServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        var env = new FakeWebHostEnvironment(_tempDir);
        _sut = new LocalFileBlobStorageService(env, NullLogger<LocalFileBlobStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task UploadLogoAsync_SavesFileAndReturnsRelativeUri()
    {
        var tenantId = Guid.NewGuid();
        using var content = new MemoryStream("png-data"u8.ToArray());

        var uri = await _sut.UploadLogoAsync(tenantId, content, "image/png");

        Assert.False(uri.IsAbsoluteUri);
        Assert.StartsWith($"/tenant-logos/{tenantId}/", uri.OriginalString);
        Assert.EndsWith(".png", uri.OriginalString);
    }

    [Fact]
    public async Task UploadLogoAsync_CreatesDirectoryIfMissing()
    {
        var tenantId = Guid.NewGuid();
        using var content = new MemoryStream("img"u8.ToArray());

        var uri = await _sut.UploadLogoAsync(tenantId, content, "image/jpeg");

        var expectedDir = Path.Combine(_tempDir, "tenant-logos", tenantId.ToString());
        Assert.True(Directory.Exists(expectedDir));
        var relativePath = uri.OriginalString.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        Assert.True(File.Exists(Path.Combine(_tempDir, relativePath)));
    }

    [Fact]
    public async Task DeleteLogoAsync_RemovesExistingFile()
    {
        var tenantId = Guid.NewGuid();
        using var content = new MemoryStream("data"u8.ToArray());
        var uri = await _sut.UploadLogoAsync(tenantId, content, "image/png");

        await _sut.DeleteLogoAsync(uri);

        var relativePath = uri.OriginalString.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        Assert.False(File.Exists(Path.Combine(_tempDir, relativePath)));
    }

    [Fact]
    public async Task DeleteLogoAsync_DoesNotThrow_WhenFileDoesNotExist()
    {
        var uri = new Uri("/tenant-logos/nonexistent/file.png", UriKind.Relative);

        var ex = await Record.ExceptionAsync(() => _sut.DeleteLogoAsync(uri));

        Assert.Null(ex);
    }

    [Fact]
    public async Task DeleteLogoAsync_HandlesAbsoluteUri()
    {
        var tenantId = Guid.NewGuid();
        using var content = new MemoryStream("data"u8.ToArray());
        var relativeUri = await _sut.UploadLogoAsync(tenantId, content, "image/png");

        // Simulate what the caller might do — build an absolute URL from the relative path
        var absoluteUri = new Uri(new Uri("http://localhost"), relativeUri);

        await _sut.DeleteLogoAsync(absoluteUri);

        var relativePath = relativeUri.OriginalString.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        Assert.False(File.Exists(Path.Combine(_tempDir, relativePath)));
    }

    private sealed class FakeWebHostEnvironment(string webRootPath) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRootPath;
        public string ContentRootPath { get; set; } = webRootPath;
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
