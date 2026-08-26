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

// WithReference(webfrontend) in AppHost injects the URL via service discovery env vars.
// Map it to the options property that KeycloakAdminClient uses when sending action emails.
builder.Services.PostConfigure<KeycloakAdminOptions>(options =>
{
    if (string.IsNullOrEmpty(options.OnboardingRedirectUri))
        options.OnboardingRedirectUri =
            builder.Configuration["services__webfrontend__https__0"] ??
            builder.Configuration["services__webfrontend__http__0"] ??
            string.Empty;
});

builder.Build().Run();
