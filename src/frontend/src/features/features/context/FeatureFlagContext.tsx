import { createContext, useContext } from 'react';
import type { ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';
import { fetchEnabledFeatures } from '../api/featuresApi';

const FeatureFlagContext = createContext<ReadonlySet<string>>(new Set());

export function FeatureFlagProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();

  const { data: flags = [] } = useQuery({
    queryKey: ['features', 'enabled'],
    queryFn: fetchEnabledFeatures,
    enabled: isAuthenticated,
    // Match the backend cache interval so the frontend and backend stay in sync.
    staleTime: 5 * 60 * 1000,
    retry: false,
  });

  return (
    <FeatureFlagContext.Provider value={new Set(flags)}>
      {children}
    </FeatureFlagContext.Provider>
  );
}

export function useFeatureFlags(): ReadonlySet<string> {
  return useContext(FeatureFlagContext);
}
