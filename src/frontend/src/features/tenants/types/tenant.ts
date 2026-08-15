/**
 * Mirrors `Heum.Data.Models.Tenant` on the server, plus a couple of
 * presentation-only fields (avatarColor) used purely for the demo UI.
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
