namespace Heum.Data.Models;

public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty; 
    public AuditAction Action { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime TimestampUtc { get; private set; }

    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    private AuditLog() { }

    public AuditLog(
        string entityName, 
        string entityId, 
        AuditAction action, 
        string userId, 
        string? oldValues, 
        string? newValues)
    {
        Id = Guid.NewGuid();
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        TimestampUtc = DateTime.UtcNow;
        OldValues = oldValues;
        NewValues = newValues;
    }
}

public enum AuditAction
{
    Create,
    Update, 
    Delete
}