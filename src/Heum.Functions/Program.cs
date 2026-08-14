using Heum.Infrastructure.Keycloak;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Aspire service defaults give us service discovery (needed to resolve "http+https://keycloak"),
// resilience handlers, health checks and OpenTelemetry wiring.
builder.AddServiceDefaults();

// KeycloakAdminClient caches its admin access token in the distributed cache.
builder.AddRedisClientBuilder("cache")
    .WithDistributedCache();

builder.AddKeycloakAdmin();

builder.Build().Run();
