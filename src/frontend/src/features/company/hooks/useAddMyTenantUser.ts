import { useMutation, useQueryClient } from '@tanstack/react-query';
import { addMyTenantUser, type AddMyTenantUserPayload } from '../api/companyApi';
import { myTenantUsersQueryKey } from './useMyTenantUsers';

export function useAddMyTenantUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: AddMyTenantUserPayload) => addMyTenantUser(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: myTenantUsersQueryKey }),
  });
}
