using Heum.Data.Auditing;

namespace Heum.Data.Models;

/// <summary>
/// Represents a single mutation (insert/update/delete) captured for an entity, independent
/// of the domain model itself. Written by <see cref="AuditingInterceptor"/>.
/// </summary>
public class AuditTrail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}
