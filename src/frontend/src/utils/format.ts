export function tenantInitials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');
}

export function formatDate(value: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export interface FieldChange {
  field: string;
  oldValue: string;
  newValue: string;
}

/**
 * Diffs the raw `oldValues`/`newValues` JSON blobs captured by the backend's audit trail
 * (`Heum.Data.Auditing.AuditTrail`) into a readable list of per-field changes. Falls back
 * to `null` if either blob isn't valid JSON, so callers can show the raw text instead.
 */
export function diffAuditValues(oldValues: string | null, newValues: string | null): FieldChange[] | null {
  try {
    const oldObj: Record<string, unknown> = oldValues ? JSON.parse(oldValues) : {};
    const newObj: Record<string, unknown> = newValues ? JSON.parse(newValues) : {};
    const fields = new Set([...Object.keys(oldObj), ...Object.keys(newObj)]);

    return Array.from(fields).map((field) => ({
      field,
      oldValue: formatAuditValue(oldObj[field]),
      newValue: formatAuditValue(newObj[field]),
    }));
  } catch {
    return null;
  }
}

function formatAuditValue(value: unknown): string {
  if (value === undefined) return '—';
  if (value === null) return 'null';
  return String(value);
}
