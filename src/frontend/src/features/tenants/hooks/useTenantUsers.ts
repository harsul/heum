import { useQuery } from '@tanstack/react-query';
import { fetchTenantUsers } from '../api/tenantsApi';

export function useTenantUsers(tenantId: string | undefined) {
  return useQuery({
    queryKey: ['tenants', tenantId, 'users'] as const,
    queryFn: () => fetchTenantUsers(tenantId!),
    enabled: Boolean(tenantId),
  });
}
