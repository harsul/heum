using Heum.Server.Data;
using Heum.Server.Services.Keycloak;

namespace Heum.Server;

// Application-specific extensions. Common Aspire wiring (service discovery, resilience,
// health checks and OpenTelemetry) lives in the Heum.ServiceDefaults project.
public static class Extensions
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<HeumdDbContext>("heumdb");

        return builder;
    }

    public static TBuilder AddKeycloakAdmin<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<KeycloakAdminOptions>()
            .Bind(builder.Configuration.GetSection(KeycloakAdminOptions.SectionName))
            .ValidateDataAnnotations();

        builder.Services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>(client =>
        {
            client.BaseAddress = new Uri("http+https://keycloak");
        });

        return builder;
    }
}
