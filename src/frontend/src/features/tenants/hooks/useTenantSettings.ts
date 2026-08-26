import { useQuery } from '@tanstack/react-query';
import { fetchTenantSettings } from '../api/tenantsApi';

export const tenantSettingsQueryKey = (id: string) => ['tenants', id, 'settings'] as const;

export function useTenantSettings(id: string | undefined) {
  return useQuery({
    queryKey: tenantSettingsQueryKey(id ?? ''),
    queryFn: () => fetchTenantSettings(id!),
    enabled: Boolean(id),
  });
}
