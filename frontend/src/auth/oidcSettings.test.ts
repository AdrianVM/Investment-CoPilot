import { describe, expect, it } from 'vitest'
import { createOidcSettings } from './oidcSettings'

const env = {
  VITE_OIDC_AUTHORITY: 'https://login.example.test/tenant',
  VITE_OIDC_CLIENT_ID: 'web-app',
} as ImportMetaEnv

describe('createOidcSettings', () => {
  it('takes the provider entirely from environment variables', () => {
    const settings = createOidcSettings(env, 'https://app.example.test')

    expect(settings.authority).toBe('https://login.example.test/tenant')
    expect(settings.client_id).toBe('web-app')
    expect(settings.redirect_uri).toBe('https://app.example.test/callback')
    expect(settings.silent_redirect_uri).toBe('https://app.example.test/silent-callback')
    expect(settings.post_logout_redirect_uri).toBe('https://app.example.test/')
  })

  it('uses the Authorization Code flow with standard scopes', () => {
    const settings = createOidcSettings(env, 'https://app.example.test')

    expect(settings.response_type).toBe('code')
    expect(settings.scope).toBe('openid profile email')
    expect(settings.automaticSilentRenew).toBe(true)
  })

  it('adds configured extra scopes', () => {
    const settings = createOidcSettings({ ...env, VITE_OIDC_EXTRA_SCOPES: 'api://x/read' }, 'https://app.example.test')

    expect(settings.scope).toBe('openid profile email api://x/read')
  })

  it('fails clearly when the provider is not configured', () => {
    expect(() => createOidcSettings({ ...env, VITE_OIDC_AUTHORITY: '' }, 'https://app.example.test')).toThrow(
      /VITE_OIDC_AUTHORITY/,
    )
  })
})
