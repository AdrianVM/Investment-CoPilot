import type { User, UserManager } from 'oidc-client-ts'
import { type ReactNode, useCallback, useEffect, useMemo, useState } from 'react'
import { AuthContext, type AuthState } from './authContext'
import { callbackPath } from './oidcSettings'

type Props = {
  userManager: UserManager
  children: ReactNode
}

/**
 * Tracks the signed-in user. On load it completes a sign-in redirect if there is one, otherwise it tries a
 * silent sign-in, because tokens are held in memory and are gone after a page reload.
 */
export function AuthProvider({ userManager, children }: Props) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<Error | null>(null)

  useEffect(() => {
    let active = true
    const onLoaded = (loaded: User) => setUser(loaded)
    const onUnloaded = () => setUser(null)
    userManager.events.addUserLoaded(onLoaded)
    userManager.events.addUserUnloaded(onUnloaded)

    async function initialise() {
      try {
        if (window.location.pathname === callbackPath) {
          const signedIn = await userManager.signinRedirectCallback()
          window.history.replaceState(null, '', returnPath(signedIn.state))
          if (active) setUser(signedIn)
          return
        }

        const existing = (await userManager.getUser()) ?? (await userManager.signinSilent().catch(() => null))
        if (active) setUser(existing && !existing.expired ? existing : null)
      } catch (e) {
        if (active) setError(e instanceof Error ? e : new Error(String(e)))
      } finally {
        if (active) setIsLoading(false)
      }
    }

    void initialise()
    return () => {
      active = false
      userManager.events.removeUserLoaded(onLoaded)
      userManager.events.removeUserUnloaded(onUnloaded)
    }
  }, [userManager])

  const signIn = useCallback(
    () => userManager.signinRedirect({ state: { returnTo: window.location.pathname + window.location.search } }),
    [userManager],
  )

  const signOut = useCallback(async () => {
    const signedIn = await userManager.getUser()
    // Discard the tokens first so the app shows the signed-out state even if the provider is unreachable.
    await userManager.removeUser()
    setUser(null)
    // Then end the provider session. The ID token hint lets the provider redirect straight back
    // instead of asking the user to confirm.
    await userManager.signoutRedirect({ id_token_hint: signedIn?.id_token }).catch(() => undefined)
  }, [userManager])

  const value = useMemo<AuthState>(
    () => ({ user, isLoading, error, signIn, signOut }),
    [user, isLoading, error, signIn, signOut],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}

function returnPath(state: unknown): string {
  const returnTo = (state as { returnTo?: unknown } | undefined)?.returnTo
  // Only same-app paths, never an absolute URL from the state.
  return typeof returnTo === 'string' && returnTo.startsWith('/') && !returnTo.startsWith('//') ? returnTo : '/'
}
