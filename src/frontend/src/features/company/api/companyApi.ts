import { apiClient } from '../../../lib/apiClient';
import type { Tenant, TenantUser } from '../../tenants/types/tenant';

export interface AddMyTenantUserPayload {
  email: string;
  role?: string;
}

export async function fetchMyTenant(): Promise<Tenant> {
  const { data } = await apiClient.get<Tenant>('/tenants/me');
  return data;
}

export async function fetchMyTenantUsers(): Promise<TenantUser[]> {
  const { data } = await apiClient.get<TenantUser[]>('/tenants/me/users');
  return data;
}

export async function addMyTenantUser(payload: AddMyTenantUserPayload): Promise<TenantUser> {
  const { data } = await apiClient.post<TenantUser>('/tenants/me/users', payload);
  return data;
}

export async function enableMyTenantUser(userId: string): Promise<void> {
  await apiClient.post(`/tenants/me/users/${userId}/enable`);
}

export async function disableMyTenantUser(userId: string): Promise<void> {
  await apiClient.post(`/tenants/me/users/${userId}/disable`);
}

export async function fetchMyTenantRoles(): Promise<string[]> {
  const { data } = await apiClient.get<string[]>('/tenants/me/roles');
  return data;
}
