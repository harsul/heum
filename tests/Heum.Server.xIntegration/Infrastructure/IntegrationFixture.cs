using Heum.Data;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
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

        // Placeholder satisfies Aspire's ValidateOnStart (non-empty check only).
        // The DbContext is replaced with in-memory below; this value is never used.
        builder.UseSetting("ConnectionStrings:heumdb",
            "Host=localhost;Database=heum-test;Username=test;Password=test");
        builder.UseSetting("ConnectionStrings:messaging",
            "Endpoint=sb://test.servicebus.windows.net/;" +
            "SharedAccessKeyName=RootManageSharedAccessKey;" +
            "SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        builder.UseSetting("ConnectionStrings:cache", "localhost:16379,abortConnect=false");
        builder.UseSetting("KeycloakAdmin:ClientSecret", "test-secret-placeholder");

        builder.ConfigureTestServices(services =>
        {
            // ── Authentication ──────────────────────────────────────────────────
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

            // ── Database ────────────────────────────────────────────────────────
            // Aspire's AddNpgsqlDbContext registers both the context options AND
            // Npgsql's IDatabaseProvider in the DI container. Calling AddDbContext
            // with UseInMemoryDatabase adds InMemory's IDatabaseProvider on top,
            // causing EF Core to throw "multiple providers registered".
            // Fix: register HeumDbContext via a scoped factory with fresh options
            // built independently of the container, so EF Core never sees the conflict.
            services.RemoveAll<DbContextOptions<HeumDbContext>>();
            services.RemoveAll<HeumDbContext>();
            services.AddScoped(_ =>
                new HeumDbContext(
                    new DbContextOptionsBuilder<HeumDbContext>()
                        .UseInMemoryDatabase("heum-test")
                        .Options));

            // ── External service fakes ──────────────────────────────────────────
            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService>(FakeKeycloak);

            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(FakeEvents);

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.RemoveAll<IOutputCacheStore>();
            services.AddOutputCache();
        });
    }

    public HttpClient CreateAuthenticatedClient(
        string roles,
        Guid? tenantId = null,
        string subject = "test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        if (tenantId.HasValue)
            client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId.Value.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        return client;
    }

    public HttpClient CreateTenantAdminClient(Guid tenantId)
        => CreateAuthenticatedClient("Admin,User", tenantId, "tenant-admin-1");

    public HttpClient CreateSystemAdminClient()
        => CreateAuthenticatedClient("SystemAdmin", subject: "sys-admin-1");
}
