import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Typography from '@mui/material/Typography';
import { Navigate } from 'react-router-dom';
import { hasRole } from '../auth/roles';

interface ProtectedRouteProps {
  children: React.ReactNode;
  /** If set, the signed-in user must also have this realm role or they're redirected away. */
  requireRole?: string;
}

export function ProtectedRoute({ children, requireRole }: ProtectedRouteProps) {
  const auth = useAuth();

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      auth.signinRedirect();
    }
  }, [auth.isLoading, auth.isAuthenticated, auth]);

  if (auth.isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          gap: 2,
        }}
      >
        <CircularProgress />
        <Typography variant="body2" color="text.secondary">
          Authenticating…
        </Typography>
      </Box>
    );
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  if (requireRole && !hasRole(auth.user, requireRole)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
