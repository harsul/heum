import { apiClient } from '../../../lib/apiClient';

export interface RecentTenantEntry {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface AdminStats {
  totalTenants: number;
  activeTenants: number;
  totalPlans: number;
  totalEntitlements: number;
  recentTenants: RecentTenantEntry[];
}

export async function fetchAdminStats(): Promise<AdminStats> {
  const { data } = await apiClient.get<AdminStats>('/admin/stats');
  return data;
}
