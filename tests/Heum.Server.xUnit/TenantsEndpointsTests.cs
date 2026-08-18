using System.Security.Claims;
using Heum.Server.Features.Tenants;

namespace Heum.Server.xUnit;

public class TenantsEndpointsTests
{
    [Fact]
    public void TryGetTenantId_ReturnsTrue_WhenClaimIsAValidGuid()
    {
        var tenantId = Guid.NewGuid();
        var principal = BuildPrincipalWithTenantId(tenantId.ToString());

        var result = TenantsEndpoints.TryGetTenantId(principal, out var parsed);

        Assert.True(result);
        Assert.Equal(tenantId, parsed);
    }

    [Fact]
    public void TryGetTenantId_ReturnsFalse_WhenClaimIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));

        var result = TenantsEndpoints.TryGetTenantId(principal, out var parsed);

        Assert.False(result);
        Assert.Equal(Guid.Empty, parsed);
    }

    [Fact]
    public void TryGetTenantId_ReturnsFalse_WhenClaimIsNotAGuid()
    {
        var principal = BuildPrincipalWithTenantId("not-a-guid");

        var result = TenantsEndpoints.TryGetTenantId(principal, out _);

        Assert.False(result);
    }

    private static ClaimsPrincipal BuildPrincipalWithTenantId(string value)
    {
        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim("tenant_id", value));
        return new ClaimsPrincipal(identity);
    }
}
