import { apiClient } from '../../../lib/apiClient';
import type { MyTenant, MyTenantUser } from '../types/company';

export interface AddMyTenantUserPayload {
  email: string;
}

export async function fetchMyTenant(): Promise<MyTenant> {
  const { data } = await apiClient.get<MyTenant>('/tenants/me');
  return data;
}

export async function fetchMyTenantUsers(): Promise<MyTenantUser[]> {
  const { data } = await apiClient.get<MyTenantUser[]>('/tenants/me/users');
  return data;
}

export async function addMyTenantUser(payload: AddMyTenantUserPayload): Promise<MyTenantUser> {
  const { data } = await apiClient.post<MyTenantUser>('/tenants/me/users', payload);
  return data;
}

export async function enableMyTenantUser(userId: string): Promise<void> {
  await apiClient.post(`/tenants/me/users/${userId}/enable`);
}

export async function disableMyTenantUser(userId: string): Promise<void> {
  await apiClient.post(`/tenants/me/users/${userId}/disable`);
}
