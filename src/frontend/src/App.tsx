import { useEffect } from 'react';
import { Routes, Route } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { DashboardPage } from './pages/DashboardPage';
import { TenantsPage } from './pages/TenantsPage';
import { TenantDetailPage } from './pages/TenantDetailPage';
import { MyCompanyPage } from './pages/MyCompanyPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { ProtectedRoute } from './components/ProtectedRoute';
import { setTokenCookie, clearTokenCookie } from './utils/cookie';
import { setAccessToken } from './lib/apiClient';
import { SYSTEM_ADMIN_ROLE, TENANT_ADMIN_ROLE } from './auth/roles';

function App() {
  const auth = useAuth();

  useEffect(() => {
    if (auth.user?.access_token) {
      setTokenCookie(auth.user.access_token);
      setAccessToken(auth.user.access_token);
    } else if (!auth.isLoading && !auth.isAuthenticated) {
      clearTokenCookie();
      setAccessToken(undefined);
    }
  }, [auth.user?.access_token, auth.isAuthenticated, auth.isLoading]);

  return (
    <Routes>
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/tenants"
        element={
          <ProtectedRoute requireRole={SYSTEM_ADMIN_ROLE}>
            <TenantsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/tenants/:id"
        element={
          <ProtectedRoute requireRole={SYSTEM_ADMIN_ROLE}>
            <TenantDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/company"
        element={
          <ProtectedRoute requireRole={TENANT_ADMIN_ROLE}>
            <MyCompanyPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;
