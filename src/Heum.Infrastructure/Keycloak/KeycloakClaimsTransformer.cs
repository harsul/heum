using System.Security.Claims;
using System.Text.Json;

namespace Heum.Infrastructure.Keycloak;

/// <summary>
/// Keycloak issues realm roles inside a single "realm_access" claim shaped like
/// { "roles": ["Admin", "User"] } rather than as individual standard role claims.
/// This helper flattens that claim into standard <see cref="ClaimTypes.Role"/> claims
/// so ASP.NET Core authorization (RequireRole/RequireAuthorization policies) works.
/// </summary>
public static class KeycloakClaimsTransformer
{
    private const string RealmAccessClaimType = "realm_access";

    public static void AddRealmRoleClaims(ClaimsIdentity identity, string? realmAccessJson)
    {
        if (string.IsNullOrWhiteSpace(realmAccessJson))
            return;

        foreach (var role in ExtractRoles(realmAccessJson))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }

    public static void AddRealmRoleClaims(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null)
            return;

        var realmAccessJson = principal.FindFirst(RealmAccessClaimType)?.Value;
        AddRealmRoleClaims(identity, realmAccessJson);
    }

    internal static IReadOnlyCollection<string> ExtractRoles(string realmAccessJson)
    {
        try
        {
            using var document = JsonDocument.Parse(realmAccessJson);
            if (!document.RootElement.TryGetProperty("roles", out var rolesElement) ||
                rolesElement.ValueKind != JsonValueKind.Array)
                return [];

            return rolesElement
                .EnumerateArray()
                .Select(role => role.GetString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
