/**
 * Generic server-side page envelope returned by all paginated endpoints.
 */
export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/**
 * Mirrors `Heum.Server.Features.Admin.Tenants.Models.TenantResponse` returned by
 * `GET /api/admin/tenants`.
 */
export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  logoUrl: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export type TenantOrder = 'asc' | 'desc';

/**
 * Mirrors `Heum.Server.Features.Admin.Tenants.Models.TenantUserResponse` returned by
 * `GET /api/admin/tenants/{id}/users`. Sourced live from Keycloak (users stamped with
 * this tenant's id), not from our own database.
 */
export interface TenantUser {
  id: string;
  username: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  enabled: boolean;
  emailVerified: boolean;
  createdAtUtc: string | null;
}

/**
 * Mirrors `Heum.Server.Features.Tenants.Models.TenantHistoryEntryResponse` returned by
 * `GET /api/admin/tenants/{id}/history` (and the self-service `/api/tenants/me/history`).
 */
export interface TenantHistoryEntry {
  id: string;
  action: 'Insert' | 'Update' | 'Delete';
  oldValues: string | null;
  newValues: string | null;
  userId: string;
  timestampUtc: string;
}

/** @deprecated Use `Page<TenantHistoryEntry>` directly. Kept for backwards compat. */
export type TenantHistoryPage = Page<TenantHistoryEntry>;

/**
 * Mirrors `Heum.Server.Features.Settings.Models.TenantSettingsResponse` returned by
 * `GET /api/settings/` (tenant admin) and `GET /api/admin/tenants/{id}/settings` (system admin).
 */
export interface TenantSettings {
  locale: string;
  timezone: string;
  updatedAtUtc: string;
}
