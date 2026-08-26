using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace Heum.Server.Extensions;

internal static class RateLimitingExtensions
{
    internal static IHostApplicationBuilder AddHeumRateLimiting(this IHostApplicationBuilder builder)
    {
        var options = builder.Configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        builder.Services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiter.AddFixedWindowLimiter("fixed", o =>
            {
                o.PermitLimit = options.Fixed.PermitLimit;
                o.Window = options.Fixed.Window;
                o.QueueLimit = options.Fixed.QueueLimit;
            });

            rateLimiter.AddFixedWindowLimiter("registration", o =>
            {
                o.PermitLimit = options.Registration.PermitLimit;
                o.Window = options.Registration.Window;
                o.QueueLimit = options.Registration.QueueLimit;
            });

            rateLimiter.AddFixedWindowLimiter("authenticated", o =>
            {
                o.PermitLimit = options.Authenticated.PermitLimit;
                o.Window = options.Authenticated.Window;
                o.QueueLimit = options.Authenticated.QueueLimit;
            });

            rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId is not null)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalAuthenticated.PermitLimit,
                        Window = options.GlobalAuthenticated.Window,
                    });
                }

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.GlobalAnonymous.PermitLimit,
                    Window = options.GlobalAnonymous.Window,
                });
            });
        });

        return builder;
    }
}
