import { apiClient } from '../../../lib/apiClient';
import type { Tenant } from '../types/tenant';

export interface UpdateTenantPayload {
  name: string;
  isActive: boolean;
}

export async function fetchTenants(): Promise<Tenant[]> {
  const { data } = await apiClient.get<Tenant[]>('/admin/tenants');
  return data;
}

export async function updateTenant(id: string, payload: UpdateTenantPayload): Promise<Tenant> {
  const { data } = await apiClient.put<Tenant>(`/admin/tenants/${id}`, payload);
  return data;
}

export async function deactivateTenant(id: string): Promise<Tenant> {
  const { data } = await apiClient.post<Tenant>(`/admin/tenants/${id}/deactivate`);
  return data;
}

export async function reactivateTenant(id: string): Promise<Tenant> {
  const { data } = await apiClient.post<Tenant>(`/admin/tenants/${id}/reactivate`);
  return data;
}
