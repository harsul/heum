import { useQuery } from '@tanstack/react-query';
import { fetchTenantOverrides, fetchTenantSubscription } from '../api/plansApi';

export const tenantSubscriptionQueryKey = (tenantId: string) =>
  ['tenantSubscription', tenantId] as const;

export const tenantOverridesQueryKey = (tenantId: string) =>
  ['tenantOverrides', tenantId] as const;

export function useTenantSubscription(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantSubscriptionQueryKey(tenantId!),
    queryFn: () => fetchTenantSubscription(tenantId!),
    enabled: !!tenantId,
  });
}

export function useTenantOverrides(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantOverridesQueryKey(tenantId!),
    queryFn: () => fetchTenantOverrides(tenantId!),
    enabled: !!tenantId,
  });
}
