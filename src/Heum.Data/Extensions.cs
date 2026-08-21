using Heum.Data.Contexts;
using Heum.Data.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heum.Data;

public static class Extensions
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddScoped<AuditingInterceptor>();

        builder.AddNpgsqlDbContext<HeumDbContext>("heumdb");

        builder.Services.AddDbContext<HeumDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditingInterceptor>();
            options.AddInterceptors(interceptor);
        });

        return builder;
    }
}