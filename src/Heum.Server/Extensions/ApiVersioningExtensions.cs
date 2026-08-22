using Asp.Versioning;

namespace Heum.Server.Extensions;

internal static class ApiVersioningExtensions
{
    private static readonly ApiVersion V1 = new(1, 0);

    internal static IServiceCollection AddHeumApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = V1;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
        });

        return services;
    }

    internal static RouteGroupBuilder MapVersionedApiGroup(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(V1)
            .ReportApiVersions()
            .Build();

        return app.MapGroup("/api")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(V1);
    }
}
