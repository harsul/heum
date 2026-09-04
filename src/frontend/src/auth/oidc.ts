import type { AuthProviderProps } from 'react-oidc-context';

declare const __KEYCLOAK_URL__: string;

export const oidcConfig: AuthProviderProps = {
  authority: `${__KEYCLOAK_URL__}/realms/saas-app`,
  client_id: 'react-frontend',
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  scope: 'openid profile email',
  onSigninCallback: () => {
    // App.tsx's useEffect hands the access token to the axios client; nothing is persisted here.
    // Strip the OIDC response params from the URL after the redirect completes.
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
