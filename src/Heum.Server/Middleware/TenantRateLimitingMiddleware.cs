using Heum.Infrastructure.Keycloak;
using Heum.Server.Configuration;
using Microsoft.Extensions.Options;

namespace Heum.Server.Middleware;

internal sealed class TenantRateLimitingMiddleware(
    RequestDelegate next,
    ITenantRateLimiter rateLimiter,
    IOptions<TenantRateLimitOptions> options,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst(KeycloakClaimTypes.TenantId)?.Value;

        if (tenantId is null)
        {
            await next(context);
            return;
        }

        var opts = options.Value;
        var nowSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var windowBucket = nowSeconds / opts.WindowSeconds;
        var key = $"rl:tenant:{tenantId}:{windowBucket}";

        var count = await rateLimiter.IncrementAsync(key, opts.WindowSeconds, context.RequestAborted);

        if (count is null)
        {
            // Redis unavailable — fail open
            await next(context);
            return;
        }

        var retryAfter = opts.WindowSeconds - (int)(nowSeconds % opts.WindowSeconds);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = opts.RequestsPerWindow.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, opts.RequestsPerWindow - count.Value).ToString();
            if (count.Value > opts.RequestsPerWindow)
                context.Response.Headers.RetryAfter = retryAfter.ToString();
            return Task.CompletedTask;
        });

        if (count.Value > opts.RequestsPerWindow)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await next(context);
    }
}
