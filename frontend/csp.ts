/**
 * Content Security Policy for the built app, added as a <meta> tag at build time.
 * Scripts and styles may only come from the app itself; the only other origins allowed are the sign-in
 * provider (token requests and the hidden silent-renew iframe) and the API, both taken from the build's
 * environment variables. Not applied to the dev server, whose hot reloading needs inline scripts.
 */
export function contentSecurityPolicy(env: Record<string, string | undefined>): string {
  const provider = originOf(env.VITE_OIDC_AUTHORITY, 'VITE_OIDC_AUTHORITY')
  const api = env.VITE_API_BASE_URL ? originOf(env.VITE_API_BASE_URL, 'VITE_API_BASE_URL') : undefined

  const directives: Record<string, string[]> = {
    'default-src': ["'self'"],
    'script-src': ["'self'"],
    'style-src': ["'self'"],
    'img-src': ["'self'", 'data:'],
    'font-src': ["'self'"],
    'connect-src': ["'self'", provider, ...(api ? [api] : [])],
    // The silent-renew iframe opens at the provider and is redirected back to the app's own callback page.
    'frame-src': ["'self'", provider],
    'form-action': ["'self'"],
    'base-uri': ["'self'"],
    'object-src': ["'none'"],
  }

  return Object.entries(directives)
    .map(([name, sources]) => `${name} ${[...new Set(sources)].join(' ')}`)
    .join('; ')
}

function originOf(url: string | undefined, name: string): string {
  if (!url) {
    throw new Error(`${name} must be set to build the Content Security Policy.`)
  }
  return new URL(url).origin
}
