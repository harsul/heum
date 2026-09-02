namespace Heum.Server.Configuration;

public sealed class TenantRateLimitOptions
{
    public int RequestsPerWindow { get; set; } = 600;
    public int WindowSeconds { get; set; } = 60;
}