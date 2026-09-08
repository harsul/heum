import { useInfiniteQuery } from '@tanstack/react-query';
import { fetchMyTenantHistory } from '../api/companyApi';

const PAGE_SIZE = 20;

export const myTenantHistoryQueryKey = ['tenants', 'me', 'history'] as const;

export function useMyTenantHistory() {
  return useInfiniteQuery({
    queryKey: myTenantHistoryQueryKey,
    queryFn: ({ pageParam }) => fetchMyTenantHistory(pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page * lastPage.pageSize < lastPage.totalCount ? lastPage.page + 1 : undefined,
  });
}