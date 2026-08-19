using System.Security.Claims;
using Heum.Infrastructure.Keycloak;

namespace Heum.Server.xUnit;

public class KeycloakClaimsHelperTests // renamed from KeycloakClaimsTransformerTests
{
    [Fact]
    public void AddRealmRoleClaims_MapsRolesFromRealmAccessClaim()
    {
        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim("realm_access", """{"roles":["SystemAdmin","User"]}"""));
        var principal = new ClaimsPrincipal(identity);

        KeycloakClaimsHelper.AddRealmRoleClaims(principal);

        Assert.True(principal.IsInRole("SystemAdmin"));
        Assert.True(principal.IsInRole("User"));
        Assert.False(principal.IsInRole("Admin"));
    }

    [Fact]
    public void AddRealmRoleClaims_NoRealmAccessClaim_DoesNotThrowAndAddsNoRoles()
    {
        var identity = new ClaimsIdentity("Bearer");
        var principal = new ClaimsPrincipal(identity);

        KeycloakClaimsHelper.AddRealmRoleClaims(principal);

        Assert.False(principal.IsInRole("SystemAdmin"));
    }

    [Fact]
    public void AddRealmRoleClaims_MalformedJson_IsIgnored()
    {
        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim("realm_access", "not-json"));
        var principal = new ClaimsPrincipal(identity);

        KeycloakClaimsHelper.AddRealmRoleClaims(principal);

        Assert.False(principal.IsInRole("SystemAdmin"));
    }
}
