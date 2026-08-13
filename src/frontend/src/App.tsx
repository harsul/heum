import { useEffect } from 'react';
import { Routes, Route } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { Dashboard } from './pages/Dashboard';
import { ProtectedRoute } from './components/ProtectedRoute';
import { setTokenCookie, clearTokenCookie } from './utils/cookie';

function App() {
  const auth = useAuth();

  useEffect(() => {
    if (auth.user?.access_token) {
      setTokenCookie(auth.user.access_token);
    } else if (!auth.isLoading && !auth.isAuthenticated) {
      clearTokenCookie();
    }
  }, [auth.user?.access_token, auth.isAuthenticated, auth.isLoading]);

  return (
    <Routes>
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default App;
