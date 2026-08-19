using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Heum.Server.xIntegration.Infrastructure;

public static class JwtTokenFactory
{
    private static readonly RsaSecurityKey _key;

    static JwtTokenFactory()
    {
        _key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key-1" };
    }

    public static RsaSecurityKey SigningKey => _key;

    /// <summary>
    /// Creates a signed JWT with a <c>realm_access</c> claim so the existing
    /// <c>KeycloakClaimsHelper.AddRealmRoleClaims</c> path is exercised in tests.
    /// </summary>
    public static string CreateToken(string subject, Guid? tenantId = null, params string[] realmRoles)
    {
        var claims = new List<Claim> { new("sub", subject) };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        if (realmRoles.Length > 0)
        {
            var rolesJson = $$"""{"roles":[{{string.Join(',', realmRoles.Select(r => $"\"{r}\""))}}]}""";
            claims.Add(new Claim("realm_access", rolesJson));
        }

        var token = new JwtSecurityToken(
            issuer: "heum-test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string TenantAdminToken(Guid tenantId, string subject = "tenant-admin-1")
        => CreateToken(subject, tenantId, "Admin", "User");

    public static string SystemAdminToken(string subject = "sys-admin-1")
        => CreateToken(subject, tenantId: null, "SystemAdmin");
}
