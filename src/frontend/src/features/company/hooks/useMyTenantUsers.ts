import { useQuery } from '@tanstack/react-query';
import { fetchMyTenantUsers } from '../api/companyApi';

export const myTenantUsersQueryKey = ['company', 'tenant', 'users'] as const;

export function useMyTenantUsers() {
  return useQuery({
    queryKey: myTenantUsersQueryKey,
    queryFn: fetchMyTenantUsers,
  });
}
