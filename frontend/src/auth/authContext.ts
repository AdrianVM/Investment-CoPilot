import type { User } from 'oidc-client-ts'
import { createContext, use } from 'react'

export type AuthState = {
  /** The signed-in user, or null when signed out. */
  user: User | null
  /** True until the initial sign-in check has finished. */
  isLoading: boolean
  /** Set if completing a sign-in failed. */
  error: Error | null
  signIn: () => Promise<void>
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const auth = use(AuthContext)
  if (!auth) {
    throw new Error('useAuth must be used inside <AuthProvider>.')
  }
  return auth
}
