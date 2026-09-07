using System.Security.Claims;
using Heum.Server.Configuration;
using Heum.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Heum.Server.xUnit;

public sealed class TenantRateLimitingMiddlewareTests
{
    private static readonly IOptions<TenantRateLimitOptions> DefaultOptions =
        Options.Create(new TenantRateLimitOptions { RequestsPerWindow = 5, WindowSeconds = 60 });

    private static DefaultHttpContext MakeContext(Guid? tenantId = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        if (tenantId is not null)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tenantId.Value.ToString())], "test"));
        }
        return ctx;
    }

    [Fact]
    public async Task Invoke_PassesThrough_WhenNoTenantClaim()
    {
        var nextCalled = false;
        var mw = new TenantRateLimitingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new FixedCountRateLimiter(1), DefaultOptions, TimeProvider.System);
        var ctx = MakeContext(); // no tenant claim

        await mw.InvokeAsync(ctx);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_PassesThrough_WhenRateLimiterReturnsNull()
    {
        var nextCalled = false;
        var mw = new TenantRateLimitingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new FailOpenRateLimiter(), DefaultOptions, TimeProvider.System);
        var ctx = MakeContext(Guid.NewGuid());

        await mw.InvokeAsync(ctx);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_PassesThrough_WhenUnderLimit()
    {
        var nextCalled = false;
        var mw = new TenantRateLimitingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new FixedCountRateLimiter(1), DefaultOptions, TimeProvider.System);
        var ctx = MakeContext(Guid.NewGuid());

        await mw.InvokeAsync(ctx);

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_Returns429_WhenOverLimit()
    {
        var mw = new TenantRateLimitingMiddleware(
            _ => Task.CompletedTask,
            new FixedCountRateLimiter(999), DefaultOptions, TimeProvider.System);
        var ctx = MakeContext(Guid.NewGuid());

        await mw.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status429TooManyRequests, ctx.Response.StatusCode);
    }

    private sealed class FixedCountRateLimiter(long count) : Heum.Server.Middleware.ITenantRateLimiter
    {
        public ValueTask<long?> IncrementAsync(string key, int windowSeconds, CancellationToken ct = default)
            => new((long?)count);
    }

    private sealed class FailOpenRateLimiter : Heum.Server.Middleware.ITenantRateLimiter
    {
        public ValueTask<long?> IncrementAsync(string key, int windowSeconds, CancellationToken ct = default)
            => new((long?)null);
    }
}
