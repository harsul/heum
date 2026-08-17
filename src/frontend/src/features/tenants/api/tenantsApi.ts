import { apiClient } from '../../../lib/apiClient';
import type { Tenant } from '../types/tenant';

export interface UpdateTenantPayload {
  name: string;
  isActive: boolean;
}

export interface CreateTenantPayload {
  companyName: string;
  slug: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
}

export async function fetchTenants(): Promise<Tenant[]> {
  const { data } = await apiClient.get<Tenant[]>('/admin/tenants');
  return data;
}

export async function fetchTenant(id: string): Promise<Tenant> {
  const { data } = await apiClient.get<Tenant>(`/admin/tenants/${id}`);
  return data;
}

export async function createTenant(payload: CreateTenantPayload): Promise<Tenant> {
  const { data } = await apiClient.post<Tenant>('/admin/tenants', payload);
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
