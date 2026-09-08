import type { ReactNode } from 'react';
import { useFeatureFlag } from '../hooks/useFeatureFlag';

interface FeatureGateProps {
  /** Feature flag name, must match the key in Azure App Configuration. */
  flag: string;
  children: ReactNode;
  /** Rendered when the flag is disabled. Defaults to nothing. */
  fallback?: ReactNode;
}

/**
 * Renders `children` only when the named feature flag is enabled for the current user/tenant.
 *
 * Usage:
 *   <FeatureGate flag="CustomDomains">
 *     <CustomDomainsPanel />
 *   </FeatureGate>
 */
export function FeatureGate({ flag, children, fallback = null }: FeatureGateProps) {
  const enabled = useFeatureFlag(flag);
  return enabled ? <>{children}</> : <>{fallback}</>;
}
