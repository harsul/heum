import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  deactivateTenant,
  reactivateTenant,
  updateTenant,
  type UpdateTenantPayload,
} from '../api/tenantsApi';
import { tenantsQueryKey } from './useTenants';

export function useUpdateTenant() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTenantPayload }) =>
      updateTenant(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantsQueryKey }),
  });
}

export function useSetTenantActive() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive ? reactivateTenant(id) : deactivateTenant(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantsQueryKey }),
  });
}
