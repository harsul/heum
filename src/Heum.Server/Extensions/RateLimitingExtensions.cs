using System.Security.Claims;
using System.Threading.RateLimiting;
using Heum.Server.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace Heum.Server.Extensions;

internal static class RateLimitingExtensions
{
    internal static IApplicationBuilder UseHeumTenantRateLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<TenantRateLimitingMiddleware>();

    internal static IServiceCollection AddHeumRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<ITenantRateLimiter, RedisTenantRateLimiter>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("fixed", o =>
            {
                o.PermitLimit = 60;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("registration", o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromMinutes(15);
                o.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("authenticated", o =>
            {
                o.PermitLimit = 120;
                o.Window = TimeSpan.FromMinutes(1);
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
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                    });
                }

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                });
            });
        });

        return services;
    }
}
