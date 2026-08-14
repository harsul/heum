using Heum.Server.Data;

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
}
