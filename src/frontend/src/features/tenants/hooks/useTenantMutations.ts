import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  deactivateTenant,
  reactivateTenant,
  updateTenant,
  type UpdateTenantPayload,
} from '../api/tenantsApi';
import { tenantsQueryKey } from './useTenants';
import { tenantQueryKey } from './useTenant';
import type { Tenant } from '../types/tenant';

export function useUpdateTenant() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTenantPayload }) =>
      updateTenant(id, payload),
    onMutate: async ({ id, payload }) => {
      await queryClient.cancelQueries({ queryKey: tenantsQueryKey });
      await queryClient.cancelQueries({ queryKey: tenantQueryKey(id) });
      const previousList = queryClient.getQueryData<Tenant[]>(tenantsQueryKey);
      const previousTenant = queryClient.getQueryData<Tenant>(tenantQueryKey(id));
      queryClient.setQueryData<Tenant[]>(tenantsQueryKey, (old) =>
        old?.map((t) => (t.id === id ? { ...t, ...payload } : t)),
      );
      queryClient.setQueryData<Tenant>(tenantQueryKey(id), (old) =>
        old ? { ...old, ...payload } : old,
      );
      return { previousList, previousTenant };
    },
    onError: (_err, { id }, context) => {
      if (context?.previousList) {
        queryClient.setQueryData(tenantsQueryKey, context.previousList);
      }
      if (context?.previousTenant) {
        queryClient.setQueryData(tenantQueryKey(id), context.previousTenant);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: tenantsQueryKey }),
  });
}

export function useSetTenantActive() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive ? reactivateTenant(id) : deactivateTenant(id),
    onMutate: async ({ id, isActive }) => {
      await queryClient.cancelQueries({ queryKey: tenantsQueryKey });
      await queryClient.cancelQueries({ queryKey: tenantQueryKey(id) });
      const previousList = queryClient.getQueryData<Tenant[]>(tenantsQueryKey);
      const previousTenant = queryClient.getQueryData<Tenant>(tenantQueryKey(id));
      queryClient.setQueryData<Tenant[]>(tenantsQueryKey, (old) =>
        old?.map((t) => (t.id === id ? { ...t, isActive } : t)),
      );
      queryClient.setQueryData<Tenant>(tenantQueryKey(id), (old) =>
        old ? { ...old, isActive } : old,
      );
      return { previousList, previousTenant };
    },
    onError: (_err, { id }, context) => {
      if (context?.previousList) {
        queryClient.setQueryData(tenantsQueryKey, context.previousList);
      }
      if (context?.previousTenant) {
        queryClient.setQueryData(tenantQueryKey(id), context.previousTenant);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: tenantsQueryKey }),
  });
}
