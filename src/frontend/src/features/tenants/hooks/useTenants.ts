import { useQuery } from '@tanstack/react-query';
import { fetchTenants } from '../api/tenantsApi';

export const tenantsQueryKey = ['tenants'] as const;

export function useTenants() {
  return useQuery({
    queryKey: tenantsQueryKey,
    queryFn: fetchTenants,
  });
}
