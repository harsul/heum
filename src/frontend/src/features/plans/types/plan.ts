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
  changedByUserId: string | null;
  effectiveAtUtc: string;
  createdAtUtc: string;
}

/** Resolved entitlements: plan defaults merged with tenant overrides. Key → value (always a string). */
export type ResolvedEntitlements = Record<string, string>;
