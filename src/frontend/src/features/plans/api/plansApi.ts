import { apiClient } from '../../../lib/apiClient';
import type {
  Entitlement,
  EntitlementType,
  Plan,
  ResolvedEntitlements,
  TenantSubscription,
} from '../types/plan';

export async function fetchPlans(): Promise<Plan[]> {
  const { data } = await apiClient.get<Plan[]>('/admin/plans');
  return data;
}

export async function fetchPlan(id: string): Promise<Plan> {
  const { data } = await apiClient.get<Plan>(`/admin/plans/${id}`);
  return data;
}

export async function createPlan(name: string): Promise<Plan> {
  const { data } = await apiClient.post<Plan>('/admin/plans', { name });
  return data;
}

export async function updatePlan(id: string, payload: { name: string; isActive: boolean }): Promise<Plan> {
  const { data } = await apiClient.put<Plan>(`/admin/plans/${id}`, payload);
  return data;
}

export async function upsertPlanEntitlement(planId: string, key: string, value: string): Promise<void> {
  await apiClient.put(`/admin/plans/${planId}/entitlements/${key}`, { value });
}

export async function fetchEntitlements(): Promise<Entitlement[]> {
  const { data } = await apiClient.get<Entitlement[]>('/admin/entitlements');
  return data;
}

export async function createEntitlement(payload: {
  key: string;
  type: EntitlementType;
  description?: string;
}): Promise<Entitlement> {
  const { data } = await apiClient.post<Entitlement>('/admin/entitlements', payload);
  return data;
}

// Subscription endpoints are nested under /admin/{tenantId}/subscription

export async function fetchCurrentSubscription(tenantId: string): Promise<TenantSubscription | null> {
  try {
    const { data } = await apiClient.get<TenantSubscription>(`/admin/${tenantId}/subscription`);
    return data;
  } catch (err: unknown) {
    if ((err as { response?: { status?: number } })?.response?.status === 404) return null;
    throw err;
  }
}

export async function fetchSubscriptionHistory(tenantId: string): Promise<TenantSubscription[]> {
  const { data } = await apiClient.get<TenantSubscription[]>(`/admin/${tenantId}/subscription/history`);
  return data;
}

export async function assignPlan(
  tenantId: string,
  payload: { planId: string; notes?: string },
): Promise<TenantSubscription> {
  const { data } = await apiClient.post<TenantSubscription>(
    `/admin/${tenantId}/subscription`,
    payload,
  );
  return data;
}

// Entitlement overrides — GET returns resolved entitlements (plan defaults merged with overrides)

export async function fetchResolvedEntitlements(tenantId: string): Promise<ResolvedEntitlements> {
  const { data } = await apiClient.get<ResolvedEntitlements>(`/admin/${tenantId}/entitlements`);
  return data;
}

export async function upsertTenantOverride(
  tenantId: string,
  key: string,
  payload: { value: string; reason?: string },
): Promise<void> {
  await apiClient.put(`/admin/${tenantId}/entitlements/${key}`, payload);
}

export async function removeTenantOverride(tenantId: string, key: string): Promise<void> {
  await apiClient.delete(`/admin/${tenantId}/entitlements/${key}`);
}
