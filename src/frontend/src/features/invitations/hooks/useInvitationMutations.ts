import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createInvitation, revokeInvitation } from '../api/invitationsApi';
import { invitationsQueryKey } from './useInvitations';

export function useCreateInvitation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (email: string) => createInvitation(email),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsQueryKey }),
  });
}

export function useRevokeInvitation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => revokeInvitation(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsQueryKey }),
  });
}
