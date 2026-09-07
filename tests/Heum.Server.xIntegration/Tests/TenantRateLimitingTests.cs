using System.Net;
using System.Threading.RateLimiting;
using Heum.Data;
using Heum.Data.Models;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
using Heum.Server.Features.Plans.Services;
using Heum.Server.Middleware;
using Heum.Server.Configuration;
using Heum.Server.xIntegration.Clients;
using Heum.Server.xIntegration.Infrastructure;
using Heum.Server.xIntegration.Infrastructure.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;

namespace Heum.Server.xIntegration.Tests;

/// <summary>
/// Uses a dedicated factory (not in IntegrationCollection) with a low per-tenant limit
/// so tests can trigger throttling without hundreds of requests.
/// </summary>
public sealed class TenantRateLimitingTests : IAsyncLifetime
{
    private readonly RateLimitFixture _fixture = new();
    private Guid _tenantId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        _ = _fixture.Server;

        await using var scope = _fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        var tenant = Tenant.Register("Rate Corp", "rate-corp", TimeProvider.System);
        var settings = TenantSettings.CreateDefault(tenant.Id, TimeProvider.System);
        db.Tenants.Add(tenant);
        db.TenantSettings.Add(settings);
        await db.SaveChangesAsync();
        _tenantId = tenant.Id;
    }

    async ValueTask IAsyncDisposable.DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task RequestsWithinLimit_Succeed()
    {
        _fixture.FakeTenantRateLimiter.Reset();
        var api = _fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenantId));

        var r1 = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);
        var r2 = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    [Fact]
    public async Task RequestsExceedingLimit_Returns429WithRetryAfter()
    {
        _fixture.FakeTenantRateLimiter.Reset();
        var api = _fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenantId));

        await api.GetMyTenantAsync(TestContext.Current.CancellationToken);
        await api.GetMyTenantAsync(TestContext.Current.CancellationToken);
        var throttled = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.True(throttled.Headers?.Contains("Retry-After") ?? false);
    }

    [Fact]
    public async Task SecondTenant_NotThrottled_WhenFirstTenantExceedsLimit()
    {
        _fixture.FakeTenantRateLimiter.Reset();
        var otherTenantId = Guid.NewGuid();

        var throttledApi = _fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenantId));
        var otherApi = _fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(otherTenantId));

        await throttledApi.GetMyTenantAsync(TestContext.Current.CancellationToken);
        await throttledApi.GetMyTenantAsync(TestContext.Current.CancellationToken);
        var throttled = await throttledApi.GetMyTenantAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        var other = await otherApi.GetMyTenantAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, other.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_NotThrottled_ByTenantLimiter()
    {
        _fixture.FakeTenantRateLimiter.Reset();
        var api = _fixture.GetClient<IAdminTenantsApi>(ClientScope.SystemAdmin);

        for (var i = 0; i < 5; i++)
        {
            var response = await api.ListTenantsAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [Fact]
    public async Task TenantAdmin_ResponseHeaders_Present()
    {
        _fixture.FakeTenantRateLimiter.Reset();
        var api = _fixture.GetClient<ITenantsApi>(ClientScope.TenantAdmin(_tenantId));

        var response = await api.GetMyTenantAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers?.Contains("X-RateLimit-Limit") ?? false);
        Assert.True(response.Headers?.Contains("X-RateLimit-Remaining") ?? false);
    }

    private sealed class RateLimitFixture : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        public InMemoryTenantRateLimiter FakeTenantRateLimiter { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });

                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultScheme             = TestAuthHandler.SchemeName;
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                    opts.DefaultForbidScheme       = TestAuthHandler.SchemeName;
                });

                services.RemoveAll<DbContextOptions<HeumDbContext>>();
                services.RemoveAll<HeumDbContext>();
                services.AddScoped(sp =>
                    new HeumDbContext(
                        new DbContextOptionsBuilder<HeumDbContext>()
                            .UseInMemoryDatabase(_dbName)
                            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                            .Options,
                        sp.GetService<Heum.Data.Multitenancy.ITenantProvider>()));

                services.RemoveAll<IKeycloakService>();
                services.AddSingleton<IKeycloakService>(new FakeKeycloakService());

                services.RemoveAll<IEventPublisher>();

                services.RemoveAll<ITenantRateLimiter>();
                services.AddSingleton<ITenantRateLimiter>(FakeTenantRateLimiter);

                services.RemoveAll<IEntitlementService>();
                services.AddSingleton<IEntitlementService, NoOpEntitlementService>();

                // TenantStatusMiddleware caches "is tenant active" in the distributed cache.
                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                // Limit of 2 so tests can trigger throttling cheaply.
                services.PostConfigure<TenantRateLimitOptions>(opts =>
                {
                    opts.RequestsPerWindow = 2;
                    opts.WindowSeconds = 60;
                });

                services.PostConfigure<RateLimiterOptions>(opts =>
                {
                    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                        RateLimitPartition.GetNoLimiter("test"));
                });

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                services.RemoveAll<IOutputCacheStore>();
                services.AddOutputCache();
            });
        }

        public T GetClient<T>(ClientScope scope)
        {
            var httpClient = scope.Roles is null
                ? CreateClient()
                : CreateAuthenticatedClient(scope.Roles, scope.TenantId, scope.Subject);
            return RestService.For<T>(httpClient);
        }

        private HttpClient CreateAuthenticatedClient(string roles, Guid? tenantId, string subject)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
            if (tenantId.HasValue)
                client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId.Value.ToString());
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
            return client;
        }
    }
}
