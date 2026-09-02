export type EntitlementType = 'Boolean' | 'Integer' | 'Decimal';

export interface PlanEntitlement {
  key: string;
  type: EntitlementType;
  value: string;
}

export interface Plan {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  entitlements: PlanEntitlement[];
}

export interface Entitlement {
  id: string;
  key: string;
  type: EntitlementType;
  description: string | null;
  isActive: boolean;
}

export interface TenantSubscription {
  id: string;
  tenantId: string;
  planId: string;
  planName: string;
  reason: string;
  notes: string | null;
  effectiveAtUtc: string;
}

export interface TenantEntitlementOverride {
  tenantId: string;
  entitlementId: string;
  entitlementKey: string;
  value: string;
  reason: string | null;
}
