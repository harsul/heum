using System.Text.Json;
using Heum.Application;
using Heum.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heum.Data.Auditing;

/// <summary>
/// Captures every insert/update/delete performed through the DbContext into a separate
/// <see cref="AuditTrail"/> table, without requiring audit properties on domain entities.
/// </summary>
public class AuditingInterceptor(ICurrentUserService currentUserService, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditTrails(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditTrails(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditTrails(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditTrail
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return;

        var userId = currentUserService.UserId;
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in entries)
        {
            var auditTrail = new AuditTrail
            {
                EntityName = entry.Entity.GetType().Name,
                PrimaryKey = GetPrimaryKeyValue(entry),
                UserId = userId,
                TimestampUtc = timestamp,
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditTrail.Action = AuditAction.Insert;
                    auditTrail.NewValues = SerializeProperties(entry.Properties, p => p.CurrentValue);
                    break;

                case EntityState.Deleted:
                    auditTrail.Action = AuditAction.Delete;
                    auditTrail.OldValues = SerializeProperties(entry.Properties, p => p.OriginalValue);
                    break;

                case EntityState.Modified:
                    var modifiedProperties = entry.Properties.Where(p => p.IsModified).ToList();
                    if (modifiedProperties.Count == 0)
                        continue;

                    auditTrail.Action = AuditAction.Update;
                    auditTrail.OldValues = SerializeProperties(modifiedProperties, p => p.OriginalValue);
                    auditTrail.NewValues = SerializeProperties(modifiedProperties, p => p.CurrentValue);
                    break;
            }

            context.Set<AuditTrail>().Add(auditTrail);
        }
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
            return string.Empty;

        var values = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null");

        return string.Join(",", values);
    }

    private static string SerializeProperties(IEnumerable<PropertyEntry> properties, Func<PropertyEntry, object?> valueSelector)
    {
        var values = properties.ToDictionary(p => p.Metadata.Name, valueSelector);

        return JsonSerializer.Serialize(values);
    }
}
