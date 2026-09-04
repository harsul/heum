import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createEntitlement } from '../api/plansApi';
import { entitlementsQueryKey } from './usePlans';
import type { EntitlementType } from '../types/plan';

export function useCreateEntitlement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { key: string; type: EntitlementType; description?: string }) =>
      createEntitlement(payload),
    onSettled: () => queryClient.invalidateQueries({ queryKey: entitlementsQueryKey }),
  });
}
