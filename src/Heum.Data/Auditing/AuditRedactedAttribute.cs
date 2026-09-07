namespace Heum.Data.Auditing;

/// <summary>
/// Marks an entity property whose value must never be written to the audit trail (secrets,
/// one-time tokens). <see cref="AuditingInterceptor"/> records the property name with a
/// placeholder value instead, so the audit row still shows that the field changed.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditRedactedAttribute : Attribute;
