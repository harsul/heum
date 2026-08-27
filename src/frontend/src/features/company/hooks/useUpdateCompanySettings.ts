import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateSettings, type UpdateSettingsPayload } from '../api/settingsApi';
import { companySettingsQueryKey } from './useCompanySettings';
import type { TenantSettings } from '../../tenants/types/tenant';

export function useUpdateCompanySettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateSettingsPayload) => updateSettings(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: companySettingsQueryKey });
      const previous = queryClient.getQueryData<TenantSettings>(companySettingsQueryKey);
      queryClient.setQueryData<TenantSettings>(companySettingsQueryKey, (old) =>
        old ? { ...old, ...payload } : old,
      );
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(companySettingsQueryKey, context.previous);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: companySettingsQueryKey }),
  });
}
