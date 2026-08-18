import { useQuery } from '@tanstack/react-query';
import { fetchTenantUsers } from '../api/tenantsApi';

export const tenantUsersQueryKey = (tenantId: string) => ['tenants', tenantId, 'users'] as const;

export function useTenantUsers(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantUsersQueryKey(tenantId ?? ''),
    queryFn: () => fetchTenantUsers(tenantId!),
    enabled: Boolean(tenantId),
  });
}
