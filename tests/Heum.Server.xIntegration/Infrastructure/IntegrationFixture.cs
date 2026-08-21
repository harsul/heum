using DotNet.Testcontainers.Builders;
using Heum.Data;
using Heum.Data.Auditing;
using Heum.Data.Domain;
using Heum.Data.Multitenancy;
using Heum.Data.SoftDelete;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
using Heum.Server.Services;
using Heum.Server.xIntegration.Infrastructure.Fakes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Refit;
using Testcontainers.PostgreSql;

namespace Heum.Server.xIntegration.Infrastructure;

public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public FakeKeycloakService FakeKeycloak { get; } = new();
    public FakeEventPublisher FakeEvents { get; } = new();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        _ = Server; // Force WAF host to build eagerly

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
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

            // PostConfigure runs last — overrides the Keycloak scheme set by main app
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
                        .UseNpgsql(_postgres.GetConnectionString())
                        .AddInterceptors(
                            sp.GetRequiredService<SoftDeleteInterceptor>(),
                            sp.GetRequiredService<AuditingInterceptor>(),
                            sp.GetRequiredService<DomainEventDispatchingInterceptor>())
                        .Options,
                    sp.GetService<ITenantProvider>()));

            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService>(FakeKeycloak);

            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(FakeEvents);

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.RemoveAll<IOutputCacheStore>();
            services.AddOutputCache();

            // Outbox draining is triggered explicitly via IOutboxProcessor in tests instead of
            // waiting on OutboxProcessorHostedService's poll interval, so tests stay deterministic
            // (and don't race the interval against test assertions/cleanup).
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IHostedService)
                                     && d.ImplementationType == typeof(OutboxProcessorHostedService))
                         .ToList())
            {
                services.Remove(descriptor);
            }
        });
    }

    /// <summary>
    /// Truncates all data tables and resets fake services between tests.
    /// Call from each test class's <see cref="IAsyncLifetime.InitializeAsync"/> instead of
    /// manually removing individual entity sets.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeumDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "Tenants", "AuditTrails", "OutboxMessages" RESTART IDENTITY CASCADE""");

        FakeEvents.Reset();
        FakeKeycloak.Reset();
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
