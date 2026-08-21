/**
 * Mirrors `Heum.Server.Features.Admin.Tenants.Models.TenantResponse` returned by
 * `GET /api/admin/tenants`.
 */
export interface Tenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
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

/**
 * Mirrors `Heum.Server.Features.Tenants.Models.TenantHistoryResponse`.
 */
export interface TenantHistoryPage {
  items: TenantHistoryEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
}
