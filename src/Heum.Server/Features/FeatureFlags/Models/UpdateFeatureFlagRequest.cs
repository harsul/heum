namespace Heum.Server.Features.FeatureFlags.Models;

public sealed record UpdateFeatureFlagRequest(
    bool IsEnabled,
    string? Description,
    IReadOnlyList<string> TargetedTenantIds,
    IReadOnlyList<string> TargetedUserIds,
    int DefaultRolloutPercentage);
