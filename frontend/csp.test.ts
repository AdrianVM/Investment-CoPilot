import { describe, expect, it } from 'vitest'
import { contentSecurityPolicy } from './csp.ts'

const directive = (policy: string, name: string) =>
  policy
    .split('; ')
    .find((d) => d.startsWith(`${name} `))
    ?.slice(name.length + 1)

describe('contentSecurityPolicy', () => {
  const policy = contentSecurityPolicy({
    VITE_OIDC_AUTHORITY: 'https://login.example.test/realms/app',
    VITE_API_BASE_URL: 'https://api.example.test/v1',
  })

  it('allows scripts and styles only from the app itself', () => {
    expect(directive(policy, 'script-src')).toBe("'self'")
    expect(directive(policy, 'style-src')).toBe("'self'")
    expect(directive(policy, 'object-src')).toBe("'none'")
  })

  it('allows connections to the sign-in provider and API origins only', () => {
    expect(directive(policy, 'connect-src')).toBe("'self' https://login.example.test https://api.example.test")
    expect(directive(policy, 'frame-src')).toBe("'self' https://login.example.test")
  })

  it('needs no API origin when the API is same-origin', () => {
    const sameOrigin = contentSecurityPolicy({ VITE_OIDC_AUTHORITY: 'https://login.example.test' })
    expect(directive(sameOrigin, 'connect-src')).toBe("'self' https://login.example.test")
  })
})
