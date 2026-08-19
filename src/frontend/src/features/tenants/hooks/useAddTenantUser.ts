import { useMutation, useQueryClient } from '@tanstack/react-query';
import { addTenantUser, type AddTenantUserPayload } from '../api/tenantsApi';
import { tenantUsersQueryKey } from './useTenantUsers';

export function useAddTenantUser(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: AddTenantUserPayload) => addTenantUser(tenantId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantUsersQueryKey(tenantId) }),
  });
}
