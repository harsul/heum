namespace Heum.Server.Features.Tenants.Models;

public class TenantHistoryEntryResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}
