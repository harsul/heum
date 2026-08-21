import { useInfiniteQuery } from '@tanstack/react-query';
import { fetchTenantHistory } from '../api/tenantsApi';

const PAGE_SIZE = 20;

export const tenantHistoryQueryKey = (tenantId: string) => ['tenants', tenantId, 'history'] as const;

export function useTenantHistory(tenantId: string | undefined) {
  return useInfiniteQuery({
    queryKey: tenantHistoryQueryKey(tenantId ?? ''),
    queryFn: ({ pageParam }) => fetchTenantHistory(tenantId!, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page * lastPage.pageSize < lastPage.totalCount ? lastPage.page + 1 : undefined,
    enabled: Boolean(tenantId),
  });
}
