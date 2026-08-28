using System.Net;
using Heum.Data;
using Heum.Data.Models;
using Heum.Server.Features.Settings.Models;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Heum.Server.xIntegration.Tests;

[Collection(nameof(IntegrationCollection))]
public class SettingsEndpointTests(IntegrationFixture fixture) : IAsyncLifetime
{
    private Guid _tenantId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = Tenant.Register("Settings Corp", "settings-corp", TimeProvider.System);
        var settings = TenantSettings.CreateDefault(tenant.Id, TimeProvider.System);
        db.Tenants.Add(tenant);
        db.TenantSettings.Add(settings);
        await db.SaveChangesAsync();
        _tenantId = tenant.Id;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;

    // --- GET /api/settings ---

    [Fact]
    public async Task GetSettings_Returns200_ForTenantAdmin()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.GetSettingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("en", response.Content!.Locale);
        Assert.Equal("UTC", response.Content.Timezone);
    }

    [Fact]
    public async Task GetSettings_Returns401_WithoutAuth()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.Anonymous);

        var response = await api.GetSettingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_Returns403_ForNonAdminRole()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.Authenticated("User", _tenantId));

        var response = await api.GetSettingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- PUT /api/settings ---

    [Fact]
    public async Task UpdateSettings_Returns200_WithUpdatedValues()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.UpdateSettingsAsync(
            new UpdateSettingsRequest { Locale = "de", Timezone = "Europe/Berlin" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("de", response.Content!.Locale);
        Assert.Equal("Europe/Berlin", response.Content.Timezone);
    }

    [Fact]
    public async Task UpdateSettings_Returns401_WithoutAuth()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.Anonymous);

        var response = await api.UpdateSettingsAsync(
            new UpdateSettingsRequest { Locale = "de", Timezone = "Europe/Berlin" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_Returns403_ForNonAdminRole()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.Authenticated("User", _tenantId));

        var response = await api.UpdateSettingsAsync(
            new UpdateSettingsRequest { Locale = "de", Timezone = "Europe/Berlin" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_PersistsChanges()
    {
        var api = fixture.GetClient<ISettingsApi>(ClientScope.TenantAdmin(_tenantId));

        await api.UpdateSettingsAsync(
            new UpdateSettingsRequest { Locale = "fr", Timezone = "Europe/Paris" },
            TestContext.Current.CancellationToken);

        var getResponse = await api.GetSettingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal("fr", getResponse.Content!.Locale);
        Assert.Equal("Europe/Paris", getResponse.Content.Timezone);
    }
}
