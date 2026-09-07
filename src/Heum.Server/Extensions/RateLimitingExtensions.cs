using System.Security.Claims;
using System.Threading.RateLimiting;
using Heum.Server.Configuration;
using Heum.Server.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace Heum.Server.Extensions;

internal static class RateLimitingExtensions
{
    internal static IApplicationBuilder UseHeumTenantRateLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<TenantRateLimitingMiddleware>();

    internal static IServiceCollection AddHeumRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var opts = configuration.GetSection(GlobalRateLimitOptions.SectionName).Get<GlobalRateLimitOptions>()
            ?? new GlobalRateLimitOptions();

        services.AddSingleton<ITenantRateLimiter, RedisTenantRateLimiter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("fixed", o =>
            {
                o.PermitLimit = opts.AnonymousPermitLimit;
                o.Window = TimeSpan.FromSeconds(opts.AnonymousWindowSeconds);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("registration", o =>
            {
                o.PermitLimit = opts.RegistrationPermitLimit;
                o.Window = TimeSpan.FromSeconds(opts.RegistrationWindowSeconds);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("authenticated", o =>
            {
                o.PermitLimit = opts.AuthenticatedPermitLimit;
                o.Window = TimeSpan.FromSeconds(opts.AuthenticatedWindowSeconds);
                o.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Keycloak emits "sub", not the WS-* NameIdentifier claim, so fall back to it —
                // otherwise every authenticated caller silently lands in the per-IP bucket.
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;
                if (userId is not null)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.AuthenticatedPermitLimit,
                        Window = TimeSpan.FromSeconds(opts.AuthenticatedWindowSeconds),
                    });
                }

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.AnonymousPermitLimit,
                    Window = TimeSpan.FromSeconds(opts.AnonymousWindowSeconds),
                });
            });
        });

        return services;
    }
}
