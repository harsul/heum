/**
 * Mirrors `Heum.Server.Features.Admin.Tenants.Models.TenantResponse` returned by
 * `GET /api/tenants/me`.
 */
export interface MyTenant {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

/**
 * Mirrors `Heum.Server.Features.Admin.Tenants.Models.TenantUserResponse` returned by
 * `GET /api/tenants/me/users`. Sourced live from Keycloak, not from our own database.
 */
export interface MyTenantUser {
  id: string;
  username: string;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  enabled: boolean;
  emailVerified: boolean;
  createdAtUtc: string | null;
}
