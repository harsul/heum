using Heum.Data.Auditing;
using Heum.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Data;

public static class Extensions
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Aspire's AddNpgsqlDbContext does not expose an IServiceProvider-aware configureDbContextOptions
        // overload, so scoped interceptors (which need DI, e.g. AuditingInterceptor -> ICurrentUserService)
        // can't be wired through it. Instead we register the DbContext the standard EF Core way and layer
        // Aspire's retries/health checks/telemetry on top via EnrichNpgsqlDbContext.
        builder.Services.AddScoped<AuditingInterceptor>();
        builder.Services.AddScoped<IDomainEventCollector, DomainEventCollector>();
        builder.Services.AddScoped<DomainEventDispatchingInterceptor>();

        builder.Services.AddDbContext<HeumDbContext>((sp, options) =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("heumdb"));
            options.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<DomainEventDispatchingInterceptor>());
        });

        builder.EnrichNpgsqlDbContext<HeumDbContext>();

        return builder;
    }
}
