import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  define: {
    __KEYCLOAK_URL__: JSON.stringify(
      process.env['KEYCLOAK_HTTP'] ?? 'http://localhost:8080'
    ),
  },
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: process.env.SERVER_HTTPS || process.env.SERVER_HTTP,
        changeOrigin: true
      }
    }
  }
});
