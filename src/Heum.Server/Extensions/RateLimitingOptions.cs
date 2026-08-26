namespace Heum.Server.Extensions;

internal sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowPolicy Fixed { get; set; } = new();
    public FixedWindowPolicy Registration { get; set; } = new();
    public FixedWindowPolicy Authenticated { get; set; } = new();
    public FixedWindowPolicy GlobalAuthenticated { get; set; } = new();
    public FixedWindowPolicy GlobalAnonymous { get; set; } = new();

    internal sealed class FixedWindowPolicy
    {
        public int PermitLimit { get; set; }
        public TimeSpan Window { get; set; }
        public int QueueLimit { get; set; }
    }
}
