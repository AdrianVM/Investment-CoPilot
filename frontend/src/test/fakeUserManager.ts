import type { User, UserManager } from 'oidc-client-ts'
import { vi } from 'vitest'

type Listener<T> = (value: T) => void

/** A stand-in for oidc-client-ts's UserManager, holding a user in memory like the real one. */
export function createFakeUserManager(initialUser: User | null = null) {
  let current = initialUser
  const loaded: Listener<User>[] = []
  const unloaded: Listener<void>[] = []

  const fake = {
    events: {
      addUserLoaded: (l: Listener<User>) => loaded.push(l),
      removeUserLoaded: (l: Listener<User>) => loaded.splice(loaded.indexOf(l), 1),
      addUserUnloaded: (l: Listener<void>) => unloaded.push(l),
      removeUserUnloaded: (l: Listener<void>) => unloaded.splice(unloaded.indexOf(l), 1),
    },
    getUser: vi.fn<() => Promise<User | null>>(async () => current),
    signinSilent: vi.fn<() => Promise<User | null>>(async () => {
      throw new Error('login_required')
    }),
    signinRedirect: vi.fn<(args?: unknown) => Promise<void>>(async () => {}),
    signinRedirectCallback: vi.fn<() => Promise<User>>(async () => {
      throw new Error('no sign-in response')
    }),
    removeUser: vi.fn<() => Promise<void>>(async () => {
      current = null
      unloaded.forEach((l) => l())
    }),
    signoutRedirect: vi.fn<(args?: { id_token_hint?: string }) => Promise<void>>(async () => {}),
  }

  return { fake, userManager: fake as unknown as UserManager }
}

export function fakeUser(overrides: Partial<User> = {}): User {
  return {
    access_token: 'access-token',
    id_token: 'id-token',
    expired: false,
    profile: { sub: 'sub-1', iss: 'https://issuer.test', aud: 'web', exp: 0, iat: 0 },
    state: undefined,
    ...overrides,
  } as User
}
