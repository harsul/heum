using Heum.Functions;
using Heum.Infrastructure.Keycloak;
using Heum.ServiceDefaults;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Aspire service defaults give us service discovery (needed to resolve "http+https://keycloak"),
// resilience handlers, health checks and OpenTelemetry wiring.
builder.AddServiceDefaults();

// KeycloakAdminClient caches its admin access token in the distributed cache.
builder.AddRedisClientBuilder("cache")
    .WithDistributedCache();

builder.AddKeycloakAdmin();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

builder.Build().Run();
