using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Configuration;

public sealed class TenantRateLimitOptions
{
    public const string SectionName = "RateLimiting:Tenant";

    [Range(1, int.MaxValue)]
    public int RequestsPerWindow { get; set; } = 600;

    // Validated so a misconfigured 0 can't reach the "now / WindowSeconds" bucket math.
    [Range(1, 86_400)]
    public int WindowSeconds { get; set; } = 60;
}
