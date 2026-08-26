import { useQuery } from '@tanstack/react-query';
import { fetchAdminAssignableRoles } from '../api/tenantsApi';

export const adminAssignableRolesQueryKey = ['admin', 'tenants', 'roles'] as const;

const TEN_MINUTES = 10 * 60 * 1000;

export function useAdminAssignableRoles() {
  return useQuery({
    queryKey: adminAssignableRolesQueryKey,
    queryFn: fetchAdminAssignableRoles,
    staleTime: TEN_MINUTES,
  });
}
