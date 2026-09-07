using System.Security.Claims;
using Heum.Server.Middleware;
using Heum.Server.xUnit.Fakes;
using Microsoft.AspNetCore.Http;

namespace Heum.Server.xUnit;

public sealed class TenantStatusMiddlewareTests
{
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
    public async Task Invoke_PassesThrough_WhenNoClaim()
    {
        var nextCalled = false;
        var middleware = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = MakeContext();

        await middleware.InvokeAsync(ctx, new FakeTenantStatusService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_PassesThrough_WhenTenantActive()
    {
        var nextCalled = false;
        var middleware = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var tenantStatus = new FakeTenantStatusService();
        var ctx = MakeContext(Guid.NewGuid());

        await middleware.InvokeAsync(ctx, tenantStatus);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_Returns403_WhenTenantInactive()
    {
        var nextCalled = false;
        var middleware = new TenantStatusMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var tenantStatus = new ConfigurableFakeTenantStatusService(isActive: false);
        var ctx = MakeContext(Guid.NewGuid());

        await middleware.InvokeAsync(ctx, tenantStatus);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, ctx.Response.StatusCode);
    }

    private sealed class ConfigurableFakeTenantStatusService(bool isActive) : Heum.Server.Features.Tenants.Services.ITenantStatusService
    {
        public ValueTask<bool> IsActiveAsync(Guid tenantId, CancellationToken cancellationToken = default) => new(isActive);
        public Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
