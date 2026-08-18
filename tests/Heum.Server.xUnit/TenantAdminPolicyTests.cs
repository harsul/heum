using System.Security.Claims;
using Heum.Infrastructure.Keycloak;

namespace Heum.Server.xUnit;

/// <summary>
/// Exercises the same "TenantAdmin" policy shape configured in Program.cs
/// (RequireRole("Admin")) against principals built the way Keycloak tokens
/// are mapped, to make sure only tenant Admins satisfy it.
/// </summary>
public class TenantAdminPolicyTests
{
    [Fact]
    public async Task Succeeds_ForPrincipalWithAdminRealmRole()
    {
        var principal = BuildPrincipalWithRealmRoles("Admin", "User");

        Assert.True(await SatisfiesTenantAdminPolicyAsync(principal));
    }

    [Fact]
    public async Task Fails_ForPrincipalWithOnlyUserRealmRole()
    {
        var principal = BuildPrincipalWithRealmRoles("User");

        Assert.False(await SatisfiesTenantAdminPolicyAsync(principal));
    }

    [Fact]
    public async Task Fails_ForPrincipalWithOnlySystemAdminRealmRole()
    {
        var principal = BuildPrincipalWithRealmRoles("SystemAdmin");

        Assert.False(await SatisfiesTenantAdminPolicyAsync(principal));
    }

    [Fact]
    public async Task Fails_ForAnonymousPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(await SatisfiesTenantAdminPolicyAsync(principal));
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

    // Mirrors what ASP.NET Core's RequireRole("Admin") policy check does under the hood
    // (RolesAuthorizationRequirement): succeed if the principal is in any of the required roles.
    private static Task<bool> SatisfiesTenantAdminPolicyAsync(ClaimsPrincipal principal) =>
        Task.FromResult(principal.IsInRole("Admin"));
}
