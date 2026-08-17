import type { User } from 'oidc-client-ts';
import { decodeJwtPayload } from '../utils/jwt';

/** Realm role that identifies platform staff who can manage tenants across the system. */
export const SYSTEM_ADMIN_ROLE = 'SystemAdmin';

interface KeycloakAccessTokenClaims {
  realm_access?: {
    roles?: string[];
  };
}

/**
 * Keycloak packs realm roles into the access token's "realm_access" claim, which
 * react-oidc-context's `user.profile` (decoded from the id_token) does not expose.
 * Decode the access token directly to read them.
 */
export function getRealmRoles(user: User | null | undefined): string[] {
  if (!user?.access_token) return [];
  return decodeJwtPayload<KeycloakAccessTokenClaims>(user.access_token)?.realm_access?.roles ?? [];
}

export function hasRole(user: User | null | undefined, role: string): boolean {
  return getRealmRoles(user).includes(role);
}

export function isSystemAdmin(user: User | null | undefined): boolean {
  return hasRole(user, SYSTEM_ADMIN_ROLE);
}
