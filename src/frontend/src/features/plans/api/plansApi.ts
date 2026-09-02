import { apiClient } from '../../../lib/apiClient';
import type {
  Entitlement,
  EntitlementType,
  Plan,
  TenantEntitlementOverride,
  TenantSubscription,
} from '../types/plan';

export async function fetchPlans(): Promise<Plan[]> {
  const { data } = await apiClient.get<{ items: Plan[] }>('/admin/plans');
  return data.items;
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
  const { data } = await apiClient.get<{ items: Entitlement[] }>('/admin/entitlements');
  return data.items;
}

export async function createEntitlement(payload: {
  key: string;
  type: EntitlementType;
  description?: string;
}): Promise<Entitlement> {
  const { data } = await apiClient.post<Entitlement>('/admin/entitlements', payload);
  return data;
}

export async function fetchTenantSubscription(
  tenantId: string,
): Promise<{ current: TenantSubscription | null; history: TenantSubscription[] }> {
  const { data } = await apiClient.get(`/admin/subscriptions/tenants/${tenantId}`);
  return data;
}

export async function assignPlan(
  tenantId: string,
  payload: { planId: string; notes?: string },
): Promise<TenantSubscription> {
  const { data } = await apiClient.post<TenantSubscription>(
    `/admin/subscriptions/tenants/${tenantId}/plan`,
    payload,
  );
  return data;
}

export async function fetchTenantOverrides(tenantId: string): Promise<TenantEntitlementOverride[]> {
  const { data } = await apiClient.get<{ items: TenantEntitlementOverride[] }>(
    `/admin/subscriptions/tenants/${tenantId}/overrides`,
  );
  return data.items;
}

export async function upsertTenantOverride(
  tenantId: string,
  key: string,
  payload: { value: string; reason?: string },
): Promise<TenantEntitlementOverride> {
  const { data } = await apiClient.put<TenantEntitlementOverride>(
    `/admin/subscriptions/tenants/${tenantId}/overrides/${key}`,
    payload,
  );
  return data;
}

export async function removeTenantOverride(tenantId: string, key: string): Promise<void> {
  await apiClient.delete(`/admin/subscriptions/tenants/${tenantId}/overrides/${key}`);
}
