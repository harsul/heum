namespace Heum.Server.Features.FeatureFlags.Models;

public sealed record EnabledFeaturesResponse(IReadOnlyList<string> EnabledFlags);
