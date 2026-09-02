import { useMutation, useQueryClient } from '@tanstack/react-query';
import { removeTenantLogo, uploadLogo } from '../api/logoApi';
import { myTenantQueryKey } from './useMyTenant';

export function useUploadLogo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => uploadLogo(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myTenantQueryKey });
    },
  });
}

export function useRemoveLogo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: removeTenantLogo,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: myTenantQueryKey });
    },
  });
}
