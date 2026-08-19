using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heum.Server.xIntegration.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Absence of the header → NoResult → 401 challenge on protected endpoints
        if (!Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var subject = Request.Headers.TryGetValue("X-Test-Subject", out var subjectHeader)
            ? subjectHeader.ToString()
            : "test-user";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new("sub", subject),
        };

        if (Request.Headers.TryGetValue("X-Test-Tenant-Id", out var tenantHeader)
            && Guid.TryParse(tenantHeader.ToString(), out _))
        {
            claims.Add(new Claim("tenant_id", tenantHeader.ToString()));
        }

        foreach (var role in rolesHeader.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
