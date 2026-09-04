import { useQuery } from '@tanstack/react-query';
import { fetchTenants, type FetchTenantsParams } from '../api/tenantsApi';

export const tenantsBaseKey = ['tenants'] as const;
export const tenantsQueryKey = (params: FetchTenantsParams) =>
  ['tenants', params] as const;

export function useTenants(params: FetchTenantsParams = {}) {
  return useQuery({
    queryKey: tenantsQueryKey(params),
    queryFn: () => fetchTenants(params),
  });
}
