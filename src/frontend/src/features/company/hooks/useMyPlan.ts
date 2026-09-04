import { useQuery } from '@tanstack/react-query';
import { fetchMyPlan } from '../api/companyApi';

export function useMyPlan() {
  return useQuery({
    queryKey: ['myPlan'],
    queryFn: fetchMyPlan,
  });
}
