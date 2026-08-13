import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const auth = useAuth();

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      auth.signinRedirect();
    }
  }, [auth.isLoading, auth.isAuthenticated, auth]);

  if (auth.isLoading) {
    return (
      <div className="app-container">
        <div className="main-content">
          <p style={{ color: 'var(--text-secondary)' }}>Authenticating...</p>
        </div>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
