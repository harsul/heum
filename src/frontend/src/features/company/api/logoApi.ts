import { apiClient } from '../../../lib/apiClient';
import type { Tenant } from '../../tenants/types/tenant';

export async function uploadLogo(file: File): Promise<Tenant> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<Tenant>('/tenants/me/logo', formData);
  return data;
}

export async function removeTenantLogo(): Promise<void> {
  await apiClient.delete('/tenants/me/logo');
}
