import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from 'react-oidc-context';
import { QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { SnackbarProvider } from 'notistack';
import { ErrorBoundary } from 'react-error-boundary';
import './index.css';
import App from './App.tsx';
import { ErrorFallback } from './components/ErrorFallback.tsx';
import { oidcConfig } from './auth/oidc.ts';
import { queryClient } from './lib/queryClient.ts';
import theme from './theme.ts';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <SnackbarProvider
        maxSnack={3}
        autoHideDuration={4000}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      />
      <ErrorBoundary FallbackComponent={ErrorFallback} onReset={() => window.location.replace('/')}>
        <AuthProvider {...oidcConfig}>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </QueryClientProvider>
        </AuthProvider>
      </ErrorBoundary>
    </ThemeProvider>
  </StrictMode>,
);
