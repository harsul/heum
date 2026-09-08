import { useInfiniteQuery } from '@tanstack/react-query';
import { fetchInvitations } from '../api/invitationsApi';

const PAGE_SIZE = 25;

export const invitationsQueryKey = ['invitations'] as const;

export function useInvitations(search?: string) {
  return useInfiniteQuery({
    queryKey: [...invitationsQueryKey, search],
    queryFn: ({ pageParam }) => fetchInvitations(pageParam, PAGE_SIZE, search),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page * lastPage.pageSize < lastPage.totalCount ? lastPage.page + 1 : undefined,
  });
}
