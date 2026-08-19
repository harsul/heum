using Heum.Data;
using Heum.Infrastructure.Keycloak.Services;
using Heum.Infrastructure.Messaging;
using Heum.Server.xIntegration.Infrastructure.Fakes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace Heum.Server.xIntegration.Infrastructure;

public sealed class IntegrationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithDatabase("heumtest")
        .WithUsername("heumtest")
        .WithPassword("heumtest")
        .Build();

    public FakeKeycloakService FakeKeycloak { get; } = new();
    public FakeEventPublisher FakeEvents { get; } = new();

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _db.StartAsync();

        // Force WAF host build now — ConfigureWebHost reads _db.GetConnectionString()
        // which is only valid after StartAsync completes above.
        _ = Server;

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HeumDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Override Aspire-injected Postgres connection string
                ["ConnectionStrings:heumdb"] = _db.GetConnectionString(),
                // Syntactically valid but never contacted — IEventPublisher is replaced below
                ["ConnectionStrings:messaging"] =
                    "Endpoint=sb://test.servicebus.windows.net/;" +
                    "SharedAccessKeyName=RootManageSharedAccessKey;" +
                    "SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
                // abortConnect=false prevents StackExchange.Redis from throwing at startup
                ["ConnectionStrings:cache"] = "localhost:16379,abortConnect=false",
                // KeycloakAdminOptions has [Required] on ClientSecret and ValidateOnStart()
                ["KeycloakAdmin:ClientSecret"] = "test-secret-placeholder",
            }));

        builder.ConfigureTestServices(services =>
        {
            // Replace IKeycloakService — prevents any HTTP calls to Keycloak
            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService>(FakeKeycloak);

            // Replace IEventPublisher — prevents Service Bus connections
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher>(FakeEvents);

            // Replace Redis-backed IDistributedCache with in-memory
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Replace Redis-backed output cache store with in-memory.
            // RemoveAll first so AddOutputCache's TryAddSingleton registers the in-memory store.
            services.RemoveAll<IOutputCacheStore>();
            services.AddOutputCache();

            // Override JWT validation — setting options.Configuration (non-null) short-circuits
            // OIDC discovery entirely; the JwtBearerHandler uses this object directly.
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.Configuration = new OpenIdConnectConfiguration { Issuer = "heum-test" };
                options.Configuration.SigningKeys.Add(JwtTokenFactory.SigningKey);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = JwtTokenFactory.SigningKey,
                };
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateTenantAdminClient(Guid tenantId)
        => CreateAuthenticatedClient(JwtTokenFactory.TenantAdminToken(tenantId));

    public HttpClient CreateSystemAdminClient()
        => CreateAuthenticatedClient(JwtTokenFactory.SystemAdminToken());
}
