import { useMutation, useQueryClient } from '@tanstack/react-query';
import { addTenantUser, type AddTenantUserPayload } from '../api/tenantsApi';

export function useAddTenantUser(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: AddTenantUserPayload) => addTenantUser(tenantId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenants', tenantId, 'users'] }),
  });
}
