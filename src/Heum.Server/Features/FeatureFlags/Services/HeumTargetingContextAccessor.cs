using Heum.Application;
using Heum.Server.Features.Tenants;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Heum.Server.Features.FeatureFlags.Services;

internal sealed class HeumTargetingContextAccessor : ITargetingContextAccessor
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public HeumTargetingContextAccessor(ICurrentUserService currentUser, ITenantContext tenantContext)
    {
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public ValueTask<TargetingContext> GetContextAsync()
    {
        // UserId and group names must match the prefixes written by AzureFeatureFlagAdminService:
        // UserId  → "user:{sub}"
        // Groups  → "tenant:{tenantId}"
        var groups = _tenantContext.HasTenant
            ? new List<string> { $"tenant:{_tenantContext.TenantId}" }
            : (IList<string>)Array.Empty<string>();

        return ValueTask.FromResult(new TargetingContext
        {
            UserId = $"user:{_currentUser.UserId}",
            Groups = groups,
        });
    }
}
