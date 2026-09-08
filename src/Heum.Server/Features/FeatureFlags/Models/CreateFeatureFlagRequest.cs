namespace Heum.Server.Features.FeatureFlags.Models;

public sealed record CreateFeatureFlagRequest(string Name, string? Description);
