import { apiClient } from '../../../lib/apiClient';
import type { TenantSettings } from '../../tenants/types/tenant';

export interface UpdateSettingsPayload {
  locale: string;
  timezone: string;
}

export async function fetchSettings(): Promise<TenantSettings> {
  const { data } = await apiClient.get<TenantSettings>('/settings/');
  return data;
}

export async function updateSettings(payload: UpdateSettingsPayload): Promise<TenantSettings> {
  const { data } = await apiClient.put<TenantSettings>('/settings/', payload);
  return data;
}
