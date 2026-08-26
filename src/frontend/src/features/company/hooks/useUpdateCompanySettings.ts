import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateSettings, type UpdateSettingsPayload } from '../api/settingsApi';
import { companySettingsQueryKey } from './useCompanySettings';

export function useUpdateCompanySettings() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateSettingsPayload) => updateSettings(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: companySettingsQueryKey }),
  });
}
