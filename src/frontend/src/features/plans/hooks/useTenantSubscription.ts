import { useQuery } from '@tanstack/react-query';
import {
  fetchCurrentSubscription,
  fetchResolvedEntitlements,
  fetchSubscriptionHistory,
} from '../api/plansApi';

export const tenantSubscriptionQueryKey = (tenantId: string) =>
  ['tenantSubscription', tenantId] as const;

export const tenantSubscriptionHistoryQueryKey = (tenantId: string) =>
  ['tenantSubscriptionHistory', tenantId] as const;

export const tenantEntitlementsQueryKey = (tenantId: string) =>
  ['tenantEntitlements', tenantId] as const;

export function useCurrentSubscription(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantSubscriptionQueryKey(tenantId!),
    queryFn: () => fetchCurrentSubscription(tenantId!),
    enabled: !!tenantId,
  });
}

export function useSubscriptionHistory(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantSubscriptionHistoryQueryKey(tenantId!),
    queryFn: () => fetchSubscriptionHistory(tenantId!),
    enabled: !!tenantId,
  });
}

export function useResolvedEntitlements(tenantId: string | undefined) {
  return useQuery({
    queryKey: tenantEntitlementsQueryKey(tenantId!),
    queryFn: () => fetchResolvedEntitlements(tenantId!),
    enabled: !!tenantId,
  });
}
