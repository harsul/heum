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
