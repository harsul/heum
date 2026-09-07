using System.ComponentModel.DataAnnotations;

namespace Heum.Server.Configuration;

public sealed class GlobalRateLimitOptions
{
    public const string SectionName = "RateLimiting:Global";

    [Range(1, int.MaxValue)]
    public int AnonymousPermitLimit { get; set; } = 60;

    [Range(1, 86_400)]
    public int AnonymousWindowSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int AuthenticatedPermitLimit { get; set; } = 120;

    [Range(1, 86_400)]
    public int AuthenticatedWindowSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int RegistrationPermitLimit { get; set; } = 5;

    [Range(1, 86_400)]
    public int RegistrationWindowSeconds { get; set; } = 900;
}
