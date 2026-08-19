using Heum.Infrastructure.Keycloak.Clients;
using Heum.Infrastructure.Keycloak.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Infrastructure.Keycloak;

public static class KeycloakExtensions
{
    public static TBuilder AddKeycloakAdmin<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<KeycloakAdminOptions>()
            .Bind(builder.Configuration.GetSection(KeycloakAdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddTransient<KeycloakAdminAuthHandler>();

        builder.Services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>(client =>
        {
            client.BaseAddress = new Uri("http+https://keycloak");
        })
        .AddHttpMessageHandler<KeycloakAdminAuthHandler>();

        builder.Services.AddScoped<IKeycloakService, KeycloakService>();

        return builder;
    }
}
