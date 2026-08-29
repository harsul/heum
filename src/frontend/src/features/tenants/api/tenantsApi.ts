import { apiClient } from '../../../lib/apiClient';
import type { Tenant, TenantHistoryPage, TenantSettings, TenantUser } from '../types/tenant';

export interface UpdateTenantPayload {
  name: string;
  isActive: boolean;
}

export interface CreateTenantPayload {
  companyName: string;
}

export async function fetchTenants(): Promise<Tenant[]> {
  const { data } = await apiClient.get<Tenant[]>('/admin/tenants');
  return data;
}

export async function fetchTenant(id: string): Promise<Tenant> {
  const { data } = await apiClient.get<Tenant>(`/admin/tenants/${id}`);
  return data;
}

export async function fetchTenantUsers(id: string): Promise<TenantUser[]> {
  const { data } = await apiClient.get<TenantUser[]>(`/admin/tenants/${id}/users`);
  return data;
}

export interface AddTenantUserPayload {
  email: string;
  role?: string;
}

export async function addTenantUser(
  tenantId: string,
  payload: AddTenantUserPayload,
): Promise<TenantUser> {
  const { data } = await apiClient.post<TenantUser>(`/admin/tenants/${tenantId}/users`, payload);
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

export async function fetchAdminAssignableRoles(): Promise<string[]> {
  const { data } = await apiClient.get<string[]>('/admin/tenants/roles');
  return data;
}

export async function fetchTenantHistory(
  id: string,
  page: number,
  pageSize: number,
): Promise<TenantHistoryPage> {
  const { data } = await apiClient.get<TenantHistoryPage>(`/admin/tenants/${id}/history`, {
    params: { page, pageSize },
  });
  return data;
}

export interface UpdateTenantSettingsPayload {
  locale: string;
  timezone: string;
}

export async function fetchTenantSettings(id: string): Promise<TenantSettings> {
  const { data } = await apiClient.get<TenantSettings>(`/admin/tenants/${id}/settings`);
  return data;
}

export async function updateTenantSettings(
  id: string,
  payload: UpdateTenantSettingsPayload,
): Promise<TenantSettings> {
  const { data } = await apiClient.put<TenantSettings>(`/admin/tenants/${id}/settings`, payload);
  return data;
}
