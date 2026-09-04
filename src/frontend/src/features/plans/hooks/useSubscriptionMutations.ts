import { useMutation, useQueryClient } from '@tanstack/react-query';
import { assignPlan, removeTenantOverride, upsertTenantOverride } from '../api/plansApi';
import {
  tenantEntitlementsQueryKey,
  tenantSubscriptionHistoryQueryKey,
  tenantSubscriptionQueryKey,
} from './useTenantSubscription';

export function useAssignPlan(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { planId: string; notes?: string }) => assignPlan(tenantId, payload),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: tenantSubscriptionQueryKey(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantSubscriptionHistoryQueryKey(tenantId) });
      queryClient.invalidateQueries({ queryKey: tenantEntitlementsQueryKey(tenantId) });
    },
  });
}

export function useUpsertOverride(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ key, value, reason }: { key: string; value: string; reason?: string }) =>
      upsertTenantOverride(tenantId, key, { value, reason }),
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: tenantEntitlementsQueryKey(tenantId) }),
  });
}

export function useRemoveOverride(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (key: string) => removeTenantOverride(tenantId, key),
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: tenantEntitlementsQueryKey(tenantId) }),
  });
}
