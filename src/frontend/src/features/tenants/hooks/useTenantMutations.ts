import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  deactivateTenant,
  reactivateTenant,
  updateTenant,
  type UpdateTenantPayload,
} from '../api/tenantsApi';
import { tenantsBaseKey } from './useTenants';
import { tenantQueryKey } from './useTenant';
import type { Tenant } from '../types/tenant';

export function useUpdateTenant() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTenantPayload }) =>
      updateTenant(id, payload),
    onMutate: async ({ id, payload }) => {
      await queryClient.cancelQueries({ queryKey: tenantQueryKey(id) });
      const previousTenant = queryClient.getQueryData<Tenant>(tenantQueryKey(id));
      queryClient.setQueryData<Tenant>(tenantQueryKey(id), (old) =>
        old ? { ...old, ...payload } : old,
      );
      return { previousTenant };
    },
    onError: (_err, { id }, context) => {
      if (context?.previousTenant) {
        queryClient.setQueryData(tenantQueryKey(id), context.previousTenant);
      }
    },
    onSettled: (_data, _err, { id }) => {
      queryClient.invalidateQueries({ queryKey: tenantsBaseKey });
      queryClient.invalidateQueries({ queryKey: tenantQueryKey(id) });
    },
  });
}

export function useSetTenantActive() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive ? reactivateTenant(id) : deactivateTenant(id),
    onMutate: async ({ id, isActive }) => {
      await queryClient.cancelQueries({ queryKey: tenantQueryKey(id) });
      const previousTenant = queryClient.getQueryData<Tenant>(tenantQueryKey(id));
      queryClient.setQueryData<Tenant>(tenantQueryKey(id), (old) =>
        old ? { ...old, isActive } : old,
      );
      return { previousTenant };
    },
    onError: (_err, { id }, context) => {
      if (context?.previousTenant) {
        queryClient.setQueryData(tenantQueryKey(id), context.previousTenant);
      }
    },
    onSettled: (_data, _err, { id }) => {
      queryClient.invalidateQueries({ queryKey: tenantsBaseKey });
      queryClient.invalidateQueries({ queryKey: tenantQueryKey(id) });
    },
  });
}
