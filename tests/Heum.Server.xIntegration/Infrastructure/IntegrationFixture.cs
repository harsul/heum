using Heum.Data;
using Heum.Data.Domain;
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

namespace Heum.Server.xIntegration.Infrastructure;

public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public FakeKeycloakService FakeKeycloak { get; } = new();
    public FakeEventPublisher FakeEvents { get; } = new();

    ValueTask IAsyncLifetime.InitializeAsync()
    {
        _ = Server; // Force WAF host to build eagerly
        return ValueTask.CompletedTask;
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
                        .UseInMemoryDatabase("heum-test")
                        // Domain events (e.g. TenantCreatedEvent) are written to the OutboxMessages
                        // table by this interceptor, same as in production - without it, nothing
                        // would ever be there for IOutboxProcessor to publish in tests.
                        .AddInterceptors(sp.GetRequiredService<DomainEventDispatchingInterceptor>())
                        .Options));

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
