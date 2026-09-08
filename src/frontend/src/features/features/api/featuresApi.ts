import { apiClient } from '../../../lib/apiClient';

export interface FeatureFlagResponse {
  name: string;
  description: string | null;
  isEnabled: boolean;
  targetedTenantIds: string[];
  targetedUserIds: string[];
  defaultRolloutPercentage: number;
}

export interface AdminFeatureFlagsResponse {
  isManageable: boolean;
  flags: FeatureFlagResponse[];
}

export interface CreateFeatureFlagRequest {
  name: string;
  description?: string;
}

export interface UpdateFeatureFlagRequest {
  isEnabled: boolean;
  description?: string | null;
  targetedTenantIds: string[];
  targetedUserIds: string[];
  defaultRolloutPercentage: number;
}

export async function fetchAdminFeatureFlags(): Promise<AdminFeatureFlagsResponse> {
  const { data } = await apiClient.get<AdminFeatureFlagsResponse>('/admin/features');
  return data;
}

export async function createFeatureFlag(request: CreateFeatureFlagRequest): Promise<FeatureFlagResponse> {
  const { data } = await apiClient.post<FeatureFlagResponse>('/admin/features', request);
  return data;
}

export async function updateFeatureFlag(name: string, request: UpdateFeatureFlagRequest): Promise<void> {
  await apiClient.put(`/admin/features/${name}`, request);
}

export async function deleteFeatureFlag(name: string): Promise<void> {
  await apiClient.delete(`/admin/features/${name}`);
}

export async function fetchEnabledFeatures(): Promise<string[]> {
  const { data } = await apiClient.get<{ enabledFlags: string[] }>('/features');
  return data.enabledFlags;
}
