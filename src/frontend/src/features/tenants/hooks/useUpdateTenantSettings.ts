import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateTenantSettings, type UpdateTenantSettingsPayload } from '../api/tenantsApi';
import { tenantSettingsQueryKey } from './useTenantSettings';

export function useUpdateTenantSettings(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateTenantSettingsPayload) => updateTenantSettings(tenantId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantSettingsQueryKey(tenantId) }),
  });
}
