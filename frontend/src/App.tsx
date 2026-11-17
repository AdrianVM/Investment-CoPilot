import { useAuth } from './auth/authContext'
import { StockLookup } from './market-data/StockLookup'
import { ProfileCard } from './profile/ProfileCard'
import { AppHeader } from './ui/AppHeader'
import { Button } from './ui/Button'
import { Notice } from './ui/Notice'

export default function App() {
  const { user, isLoading, error, signIn, signOut } = useAuth()
  const userName = user ? (user.profile.name ?? user.profile.email ?? null) : null

  return (
    <>
      <AppHeader
        userName={userName}
        signedIn={user !== null}
        checking={isLoading}
        onSignIn={() => void signIn()}
        onSignOut={() => void signOut()}
      />
      <main className="page">
        {error && <Notice tone="danger">Signing in failed: {error.message}</Notice>}
        {isLoading ? (
          <p className="page__status">Checking your sign-in…</p>
        ) : user ? (
          <>
            <h1 className="visually-hidden">Investment Co-Pilot</h1>
            <StockLookup />
            <ProfileCard />
          </>
        ) : (
          <SignedOut onSignIn={() => void signIn()} />
        )}
      </main>
    </>
  )
}

function SignedOut({ onSignIn }: { onSignIn: () => void }) {
  return (
    <section className="intro" aria-labelledby="intro-heading">
      <h1 id="intro-heading" className="intro__title">
        Read a company’s key figures in one place
      </h1>
      <p className="intro__text">
        Look up a stock by its ticker symbol to see its price, valuation, earnings growth, ownership and debt: the
        figures Peter Lynch checked before buying.
      </p>
      <Button variant="primary" onClick={onSignIn}>
        Sign in
      </Button>
    </section>
  )
}
