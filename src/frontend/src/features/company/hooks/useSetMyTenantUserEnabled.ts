import { useMutation, useQueryClient } from '@tanstack/react-query';
import { disableMyTenantUser, enableMyTenantUser } from '../api/companyApi';
import { myTenantUsersQueryKey } from './useMyTenantUsers';
import type { TenantUser } from '../../tenants/types/tenant';

export function useSetMyTenantUserEnabled() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, enabled }: { userId: string; enabled: boolean }) =>
      enabled ? enableMyTenantUser(userId) : disableMyTenantUser(userId),
    onMutate: async ({ userId, enabled }) => {
      await queryClient.cancelQueries({ queryKey: myTenantUsersQueryKey });
      const previous = queryClient.getQueryData<TenantUser[]>(myTenantUsersQueryKey);
      queryClient.setQueryData<TenantUser[]>(myTenantUsersQueryKey, (old) =>
        old?.map((u) => (u.id === userId ? { ...u, enabled } : u)),
      );
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(myTenantUsersQueryKey, context.previous);
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: myTenantUsersQueryKey }),
  });
}
