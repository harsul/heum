using System.Security.Claims;
using Heum.Infrastructure.Keycloak;

namespace Heum.Server.xUnit;

/// <summary>
/// Exercises the same "SystemAdmin" policy shape configured in Program.cs
/// (RequireRole("SystemAdmin")) against principals built the way Keycloak
/// tokens are mapped, to make sure only SystemAdmin users satisfy it.
/// </summary>
public class SystemAdminPolicyTests
{
    [Fact]
    public async Task Succeeds_ForPrincipalWithSystemAdminRealmRole()
    {
        var principal = BuildPrincipalWithRealmRoles("SystemAdmin");

        Assert.True(await SatisfiesSystemAdminPolicyAsync(principal));
    }

    [Fact]
    public async Task Fails_ForPrincipalWithoutSystemAdminRealmRole()
    {
        var principal = BuildPrincipalWithRealmRoles("Admin", "User");

        Assert.False(await SatisfiesSystemAdminPolicyAsync(principal));
    }

    [Fact]
    public async Task Fails_ForAnonymousPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(await SatisfiesSystemAdminPolicyAsync(principal));
    }

    private static ClaimsPrincipal BuildPrincipalWithRealmRoles(params string[] roles)
    {
        var identity = new ClaimsIdentity("Bearer");
        var rolesJson = $$"""{"roles":[{{string.Join(',', roles.Select(r => $"\"{r}\""))}}]}""";
        identity.AddClaim(new Claim("realm_access", rolesJson));

        var principal = new ClaimsPrincipal(identity);
        KeycloakClaimsTransformer.AddRealmRoleClaims(principal);

        return principal;
    }

    // Mirrors what ASP.NET Core's RequireRole("SystemAdmin") policy check does under the
    // hood (RolesAuthorizationRequirement): succeed if the principal is in any of the
    // required roles.
    private static Task<bool> SatisfiesSystemAdminPolicyAsync(ClaimsPrincipal principal) =>
        Task.FromResult(principal.IsInRole("SystemAdmin"));
}
