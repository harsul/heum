import { useFeatureFlags } from '../features/features/context/FeatureFlagContext';

/**
 * Returns true when the named feature flag is enabled for the current user/tenant.
 * Evaluation happens server-side via Microsoft.FeatureManagement targeting;
 * this hook just reads the cached result fetched at login.
 *
 * Usage:
 *   const hasDomains = useFeatureFlag('CustomDomains');
 *   if (hasDomains) { ... }
 */
export function useFeatureFlag(name: string): boolean {
  return useFeatureFlags().has(name);
}
