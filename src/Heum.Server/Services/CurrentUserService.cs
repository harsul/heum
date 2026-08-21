using System.Security.Claims;
using Heum.Data.Auditing;

namespace Heum.Server.Services;

/// <summary>
/// Resolves the current user's identifier from the HTTP context's <see cref="ClaimsPrincipal"/>.
/// Falls back to "System" for unauthenticated requests or when there is no HTTP context at all
/// (e.g. background jobs, migrations), so auditing never fails on that basis.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private const string SystemUserId = "System";

    public string UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return SystemUserId;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            return string.IsNullOrWhiteSpace(userId) ? SystemUserId : userId;
        }
    }
}
