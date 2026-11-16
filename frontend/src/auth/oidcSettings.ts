import { InMemoryWebStorage, type UserManagerSettings, WebStorageStateStore } from 'oidc-client-ts'

/** Paths the provider redirects back to. They must be allowed as redirect URIs at the provider. */
export const callbackPath = '/callback'
export const silentCallbackPath = '/silent-callback'

/**
 * Builds the sign-in settings from Vite environment variables only, so any standards-compliant
 * OpenID Connect provider works without code changes.
 */
export function createOidcSettings(env: ImportMetaEnv, origin: string): UserManagerSettings {
  const authority = env.VITE_OIDC_AUTHORITY
  const clientId = env.VITE_OIDC_CLIENT_ID
  if (!authority || !clientId) {
    throw new Error('VITE_OIDC_AUTHORITY and VITE_OIDC_CLIENT_ID must be set.')
  }

  return {
    authority,
    client_id: clientId,
    redirect_uri: `${origin}${callbackPath}`,
    silent_redirect_uri: `${origin}${silentCallbackPath}`,
    post_logout_redirect_uri: `${origin}/`,
    // Authorization Code flow; oidc-client-ts always uses PKCE (S256) with it.
    response_type: 'code',
    scope: ['openid', 'profile', 'email', env.VITE_OIDC_EXTRA_SCOPES].filter(Boolean).join(' '),
    // Tokens are kept in memory only, never in localStorage or sessionStorage.
    userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
    // Renew the access token in a hidden iframe before it expires.
    automaticSilentRenew: true,
  }
}
