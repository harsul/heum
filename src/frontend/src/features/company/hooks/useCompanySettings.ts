import { useQuery } from '@tanstack/react-query';
import { fetchSettings } from '../api/settingsApi';

export const companySettingsQueryKey = ['company', 'settings'] as const;

export function useCompanySettings() {
  return useQuery({
    queryKey: companySettingsQueryKey,
    queryFn: fetchSettings,
  });
}
