using Heum.Application;
using Heum.Server.Features.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Heum.Server.Features.FeatureFlags.Services;

internal sealed class HeumTargetingContextAccessor : ITargetingContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeumTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ValueTask<TargetingContext> GetContextAsync()
    {
        var services = _httpContextAccessor.HttpContext?.RequestServices;
        var currentUser = services?.GetService<ICurrentUserService>();
        var tenantContext = services?.GetService<ITenantContext>();

        // UserId and group names must match the prefixes written by AzureFeatureFlagAdminService:
        // UserId  → "user:{sub}"
        // Groups  → "tenant:{tenantId}"
        var groups = tenantContext?.HasTenant == true
            ? new List<string> { $"tenant:{tenantContext.TenantId}" }
            : (IList<string>)Array.Empty<string>();

        return ValueTask.FromResult(new TargetingContext
        {
            UserId = $"user:{currentUser?.UserId}",
            Groups = groups,
        });
    }
}
