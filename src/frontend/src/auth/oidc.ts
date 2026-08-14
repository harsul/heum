import type { AuthProviderProps } from 'react-oidc-context';
import { setTokenCookie } from '../utils/cookie';

declare const __KEYCLOAK_URL__: string;

export const oidcConfig: AuthProviderProps = {
  authority: `${__KEYCLOAK_URL__}/realms/saas-app`,
  client_id: 'react-frontend',
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  scope: 'openid profile email',
  onSigninCallback: (user) => {
    if (user?.access_token) {
      setTokenCookie(user.access_token);
    }
    window.history.replaceState({}, document.title, window.location.pathname);
  },
};
