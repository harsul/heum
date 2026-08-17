import type { Tenant, TenantOrder } from './types/tenant';

type ComparableKey = keyof Pick<Tenant, 'name' | 'slug' | 'createdAtUtc' | 'isActive'>;

function descendingComparator(a: Tenant, b: Tenant, orderBy: ComparableKey) {
  if (b[orderBy] < a[orderBy]) return -1;
  if (b[orderBy] > a[orderBy]) return 1;
  return 0;
}

export function getComparator(
  order: TenantOrder,
  orderBy: ComparableKey,
): (a: Tenant, b: Tenant) => number {
  return order === 'desc'
    ? (a, b) => descendingComparator(a, b, orderBy)
    : (a, b) => -descendingComparator(a, b, orderBy);
}

export function applyTenantFilter({
  tenants,
  comparator,
  query,
}: {
  tenants: Tenant[];
  comparator: (a: Tenant, b: Tenant) => number;
  query: string;
}) {
  const stabilized = tenants.map((tenant, index) => [tenant, index] as const);
  stabilized.sort((a, b) => {
    const order = comparator(a[0], b[0]);
    return order !== 0 ? order : a[1] - b[1];
  });
  const sorted = stabilized.map((entry) => entry[0]);

  if (!query) return sorted;

  const lowerQuery = query.toLowerCase();
  return sorted.filter(
    (tenant) =>
      tenant.name.toLowerCase().includes(lowerQuery) ||
      tenant.slug.toLowerCase().includes(lowerQuery),
  );
}

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
