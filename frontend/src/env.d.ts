interface ImportMetaEnv {
  /** Issuer URL of the OpenID Connect provider. */
  readonly VITE_OIDC_AUTHORITY: string
  /** Client ID of this web app at the provider (a public client using Authorization Code + PKCE). */
  readonly VITE_OIDC_CLIENT_ID: string
  /** Extra scopes to request besides `openid profile email`, space-separated. Optional. */
  readonly VITE_OIDC_EXTRA_SCOPES?: string
  /** Base URL of the API. Empty for same origin. */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
