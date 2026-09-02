import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createPlan, updatePlan, upsertPlanEntitlement } from '../api/plansApi';
import { planQueryKey, plansQueryKey } from './usePlans';

export function useCreatePlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => createPlan(name),
    onSettled: () => queryClient.invalidateQueries({ queryKey: plansQueryKey }),
  });
}

export function useUpdatePlan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: { name: string; isActive: boolean } }) =>
      updatePlan(id, payload),
    onSettled: (_data, _err, { id }) => {
      queryClient.invalidateQueries({ queryKey: planQueryKey(id) });
      queryClient.invalidateQueries({ queryKey: plansQueryKey });
    },
  });
}

export function useUpsertPlanEntitlement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ planId, key, value }: { planId: string; key: string; value: string }) =>
      upsertPlanEntitlement(planId, key, value),
    onSettled: (_data, _err, { planId }) => {
      queryClient.invalidateQueries({ queryKey: planQueryKey(planId) });
    },
  });
}
