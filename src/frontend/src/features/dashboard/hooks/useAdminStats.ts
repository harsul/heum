import { useQuery } from '@tanstack/react-query';
import { fetchAdminStats } from '../api/dashboardApi';

export const adminStatsQueryKey = ['admin-stats'] as const;

export function useAdminStats() {
  return useQuery({
    queryKey: adminStatsQueryKey,
    queryFn: fetchAdminStats,
  });
}
