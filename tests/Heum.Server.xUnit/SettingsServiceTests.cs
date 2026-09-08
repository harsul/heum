using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Settings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Server.xUnit;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly HeumDbContext _db;
    private readonly SettingsService _service;
    private static readonly Guid TenantId = Guid.NewGuid();

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<HeumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new HeumDbContext(options);
        _service = new SettingsService(_db, TimeProvider.System);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNoSettingsExist()
    {
        var result = await _service.GetAsync(TenantId, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsExistingSettings()
    {
        var settings = TenantSettings.CreateDefault(TenantId, TimeProvider.System);
        _db.TenantSettings.Add(settings);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetAsync(TenantId, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(TenantId, result.TenantId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNoSettingsExist()
    {
        var result = await _service.UpdateAsync(TenantId, "fr", "Europe/Paris", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingSettings()
    {
        var settings = TenantSettings.CreateDefault(TenantId, TimeProvider.System);
        _db.TenantSettings.Add(settings);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.UpdateAsync(TenantId, "de", "Europe/Berlin", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("de", result.Locale);
        Assert.Equal("Europe/Berlin", result.Timezone);
    }
}
