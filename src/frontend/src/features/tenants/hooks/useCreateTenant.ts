import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createTenant } from '../api/tenantsApi';
import { tenantsQueryKey } from './useTenants';

export function useCreateTenant() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createTenant,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantsQueryKey }),
  });
}
