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
