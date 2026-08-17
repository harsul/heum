import { useQuery } from '@tanstack/react-query';
import { fetchTenant } from '../api/tenantsApi';

export function useTenant(id: string | undefined) {
  return useQuery({
    queryKey: ['tenants', id] as const,
    queryFn: () => fetchTenant(id!),
    enabled: Boolean(id),
  });
}
