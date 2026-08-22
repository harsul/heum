namespace Heum.Server.Features.Settings.Models;

public sealed class TenantSettingsResponse
{
    public required string Locale { get; init; }
    public required string Timezone { get; init; }
    public required DateTime UpdatedAtUtc { get; init; }
}
