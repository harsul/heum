import { useMutation, useQueryClient } from '@tanstack/react-query';
import { confirmLogoUrl, fetchLogoUploadUrl, removeTenantLogo, uploadToBlob } from '../api/logoApi';
import { myTenantQueryKey } from './useMyTenant';

export function useUploadLogo() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (file: File) => {
      const { uploadUrl, blobUrl } = await fetchLogoUploadUrl(file.type);
      await uploadToBlob(uploadUrl, file);
      return confirmLogoUrl(blobUrl);
    },
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
