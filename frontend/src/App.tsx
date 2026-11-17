import { useAuth } from './auth/authContext'
import { StockLookup } from './market-data/StockLookup'
import { ProfileCard } from './profile/ProfileCard'

export default function App() {
  const { user, isLoading, error, signIn, signOut } = useAuth()

  return (
    <>
      <header className="app-header">
        <h1>Investment Co-Pilot</h1>
        {!isLoading &&
          (user ? (
            <button type="button" onClick={() => void signOut()}>
              Sign out
            </button>
          ) : (
            <button type="button" onClick={() => void signIn()}>
              Sign in
            </button>
          ))}
      </header>
      <main>
        {error && <p role="alert">Signing in failed: {error.message}</p>}
        {isLoading ? (
          <p>Checking your sign-in…</p>
        ) : user ? (
          <>
            <ProfileCard />
            <StockLookup />
          </>
        ) : (
          <p>Sign in to see your profile.</p>
        )}
      </main>
    </>
  )
}
