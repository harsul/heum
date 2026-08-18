import { useQuery } from '@tanstack/react-query';
import { fetchMyTenant } from '../api/companyApi';

export const myTenantQueryKey = ['company', 'tenant'] as const;

export function useMyTenant() {
  return useQuery({
    queryKey: myTenantQueryKey,
    queryFn: fetchMyTenant,
  });
}
