import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateTenantSettings, type UpdateTenantSettingsPayload } from '../api/tenantsApi';
import { tenantSettingsQueryKey } from './useTenantSettings';
import type { TenantSettings } from '../types/tenant';

export function useUpdateTenantSettings(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateTenantSettingsPayload) => updateTenantSettings(tenantId, payload),
    onMutate: async (payload) => {
      const queryKey = tenantSettingsQueryKey(tenantId);
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<TenantSettings>(queryKey);
      queryClient.setQueryData<TenantSettings>(queryKey, (old) =>
        old ? { ...old, ...payload } : old,
      );
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(tenantSettingsQueryKey(tenantId), context.previous);
      }
    },
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: tenantSettingsQueryKey(tenantId) }),
  });
}
