import { useQuery } from '@tanstack/react-query';
import { fetchMyPlan, fetchMySubscriptionHistory } from '../api/companyApi';

export function useMyPlan() {
  return useQuery({
    queryKey: ['myPlan'],
    queryFn: fetchMyPlan,
  });
}

export function useMySubscriptionHistory() {
  return useQuery({
    queryKey: ['myPlanHistory'],
    queryFn: fetchMySubscriptionHistory,
  });
}
