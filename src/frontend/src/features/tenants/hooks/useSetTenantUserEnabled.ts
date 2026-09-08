import { useMutation, useQueryClient } from '@tanstack/react-query';
import { enableTenantUser, disableTenantUser } from '../api/tenantsApi';
import { tenantUsersQueryKey } from './useTenantUsers';
import type { TenantUser } from '../types/tenant';

export function useSetTenantUserEnabled(tenantId: string) {
  const queryClient = useQueryClient();
  const qKey = tenantUsersQueryKey(tenantId);

  return useMutation({
    mutationFn: ({ userId, enabled }: { userId: string; enabled: boolean }) =>
      enabled ? enableTenantUser(tenantId, userId) : disableTenantUser(tenantId, userId),
    onMutate: async ({ userId, enabled }) => {
      await queryClient.cancelQueries({ queryKey: qKey });
      const previous = queryClient.getQueryData<TenantUser[]>(qKey);
      queryClient.setQueryData<TenantUser[]>(qKey, (old) =>
        old?.map((u) => (u.id === userId ? { ...u, enabled } : u)),
      );
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) queryClient.setQueryData(qKey, context.previous);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: qKey }),
  });
}