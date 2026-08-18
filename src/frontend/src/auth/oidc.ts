import type { AuthProviderProps } from 'react-oidc-context';

declare const __KEYCLOAK_URL__: string;

export const oidcConfig: AuthProviderProps = {
  authority: `${__KEYCLOAK_URL__}/realms/saas-app`,
  client_id: 'react-frontend',
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  scope: 'openid profile email',
  onSigninCallback: () => {
    // Token is written to the cookie by App.tsx's useEffect (the single source of truth).
    // The cookie is read server-side by the ASP.NET backend, not by frontend JS.
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
