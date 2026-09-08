namespace Heum.Server.Features.FeatureFlags.Models;

public sealed record FeatureFlagResponse(
    string Name,
    string? Description,
    bool IsEnabled,
    IReadOnlyList<string> TargetedTenantIds,
    IReadOnlyList<string> TargetedUserIds,
    int DefaultRolloutPercentage);
