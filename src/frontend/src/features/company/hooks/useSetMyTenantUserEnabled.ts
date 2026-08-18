import { useMutation, useQueryClient } from '@tanstack/react-query';
import { disableMyTenantUser, enableMyTenantUser } from '../api/companyApi';
import { myTenantUsersQueryKey } from './useMyTenantUsers';

export function useSetMyTenantUserEnabled() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, enabled }: { userId: string; enabled: boolean }) =>
      enabled ? enableMyTenantUser(userId) : disableMyTenantUser(userId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: myTenantUsersQueryKey }),
  });
}
