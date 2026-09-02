import { useQuery } from '@tanstack/react-query';
import { fetchEntitlements, fetchPlan, fetchPlans } from '../api/plansApi';

export const plansQueryKey = ['plans'] as const;
export const planQueryKey = (id: string) => ['plans', id] as const;
export const entitlementsQueryKey = ['entitlements'] as const;

export function usePlans() {
  return useQuery({
    queryKey: plansQueryKey,
    queryFn: fetchPlans,
  });
}

export function usePlan(id: string | undefined) {
  return useQuery({
    queryKey: planQueryKey(id!),
    queryFn: () => fetchPlan(id!),
    enabled: !!id,
  });
}

export function useEntitlements() {
  return useQuery({
    queryKey: entitlementsQueryKey,
    queryFn: fetchEntitlements,
  });
}
