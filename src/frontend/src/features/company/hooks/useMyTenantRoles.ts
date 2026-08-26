import { useQuery } from '@tanstack/react-query';
import { fetchMyTenantRoles } from '../api/companyApi';

export const myTenantRolesQueryKey = ['company', 'tenant', 'roles'] as const;

const TEN_MINUTES = 10 * 60 * 1000;

export function useMyTenantRoles() {
  return useQuery({
    queryKey: myTenantRolesQueryKey,
    queryFn: fetchMyTenantRoles,
    staleTime: TEN_MINUTES,
  });
}
