import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createTenant } from '../api/tenantsApi';
import { tenantsBaseKey } from './useTenants';

export function useCreateTenant() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createTenant,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantsBaseKey }),
  });
}
