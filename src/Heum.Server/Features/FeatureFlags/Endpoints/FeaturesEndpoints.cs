using Heum.Server.Features.FeatureFlags.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.FeatureManagement;

namespace Heum.Server.Features.FeatureFlags.Endpoints;

public static class FeaturesEndpoints
{
    public static RouteGroupBuilder MapFeaturesEndpoints(this RouteGroupBuilder group)
    {
        // Returns the subset of flags that are enabled for the calling user/tenant.
        // Evaluated via IFeatureManager which uses HeumTargetingContextAccessor under the hood.
        group.MapGet("/features", GetEnabledFeaturesAsync)
            .WithName("GetEnabledFeatures")
            .RequireAuthorization();
        return group;
    }

    internal static async Task<Ok<EnabledFeaturesResponse>> GetEnabledFeaturesAsync(
        IFeatureManager featureManager,
        CancellationToken ct)
    {
        var enabled = new List<string>();

        await foreach (var name in featureManager.GetFeatureNamesAsync())
        {
            if (await featureManager.IsEnabledAsync(name))
                enabled.Add(name);
        }

        return TypedResults.Ok(new EnabledFeaturesResponse(enabled));
    }
}
