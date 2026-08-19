import { useQuery } from '@tanstack/react-query';
import { fetchTenant } from '../api/tenantsApi';

export const tenantQueryKey = (id: string) => ['tenants', id] as const;

export function useTenant(id: string | undefined) {
  return useQuery({
    queryKey: tenantQueryKey(id ?? ''),
    queryFn: () => fetchTenant(id!),
    enabled: Boolean(id),
  });
}
