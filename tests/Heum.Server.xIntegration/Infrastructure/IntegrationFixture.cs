using System.Threading.RateLimiting;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Data.Domain;
using Heum.Data.Models;
using Heum.Data.Multitenancy;
using Heum.Data.SoftDelete;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
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
using Testcontainers.PostgreSql;

namespace Heum.Server.xIntegration.Infrastructure;

public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static bool UseTestcontainers =>
        Environment.GetEnvironmentVariable("USE_TESTCONTAINERS") == "true"
        || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    private readonly PostgreSqlContainer? _postgres = UseTestcontainers
        ? new PostgreSqlBuilder("postgres:17-alpine").Build()
        : null;

    private readonly string _inMemoryDbName = Guid.NewGuid().ToString();

    public FakeKeycloakService FakeKeycloak { get; } = new();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        if (_postgres is not null)
            await _postgres.StartAsync();

        _ = Server;

        if (_postgres is not null)
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
            await db.Database.MigrateAsync();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

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

            if (_postgres is not null)
            {
                services.AddScoped(sp =>
                    new HeumDbContext(
                        new DbContextOptionsBuilder<HeumDbContext>()
                            .UseNpgsql(_postgres.GetConnectionString())
                            .AddInterceptors(
                                sp.GetRequiredService<SoftDeleteInterceptor>(),
                                sp.GetRequiredService<AuditingInterceptor>(),
                                sp.GetRequiredService<DomainEventDispatchingInterceptor>())
                            .Options,
                        sp.GetService<ITenantProvider>()));
            }
            else
            {
                services.AddScoped(sp =>
                    new HeumDbContext(
                        new DbContextOptionsBuilder<HeumDbContext>()
                            .UseInMemoryDatabase(_inMemoryDbName)
                            .AddInterceptors(
                                sp.GetRequiredService<SoftDeleteInterceptor>(),
                                sp.GetRequiredService<AuditingInterceptor>(),
                                sp.GetRequiredService<DomainEventDispatchingInterceptor>())
                            .Options,
                        sp.GetService<ITenantProvider>()));
            }

            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService>(FakeKeycloak);

            services.RemoveAll<IEventPublisher>();

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

    /// <summary>
    /// Clears all data and resets fake services between tests.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();

        if (_postgres is not null)
        {
            await db.Database.ExecuteSqlRawAsync(
                """TRUNCATE TABLE "Tenants", "TenantSettings", "Invitations", "AuditTrails", "OutboxMessages" RESTART IDENTITY CASCADE""");
        }
        else
        {
            db.Set<AuditTrail>().RemoveRange(db.Set<AuditTrail>());
            db.OutboxMessages.RemoveRange(db.OutboxMessages);
            db.Invitations.RemoveRange(db.Invitations.IgnoreQueryFilters());
            db.TenantSettings.RemoveRange(db.TenantSettings.IgnoreQueryFilters());
            db.Tenants.RemoveRange(db.Tenants);
            await db.SaveChangesAsync();
        }

        FakeKeycloak.Reset();
    }

    public async Task ClearAuditTrailsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();

        if (_postgres is not null)
        {
            await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "AuditTrails" """);
        }
        else
        {
            db.Set<AuditTrail>().RemoveRange(db.Set<AuditTrail>());
            await db.SaveChangesAsync();
        }
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
