import { AppMark } from './AppMark'
import { Button } from './Button'

type Props = {
  /** The signed-in user's display name or email; null when signed out. */
  userName: string | null
  signedIn: boolean
  /** While the sign-in state is being checked, no action is offered. */
  checking: boolean
  onSignIn: () => void
  onSignOut: () => void
}

export function AppHeader({ userName, signedIn, checking, onSignIn, onSignOut }: Props) {
  return (
    <header className="app-header">
      <div className="app-header__inner">
        <span className="app-header__brand">
          <AppMark />
          <span className="app-header__name">Investment Co-Pilot</span>
        </span>
        {!checking && (
          <div className="app-header__account">
            {signedIn && userName && <span className="app-header__user">{userName}</span>}
            {signedIn ? (
              <Button onClick={onSignOut}>Sign out</Button>
            ) : (
              <Button variant="primary" onClick={onSignIn}>
                Sign in
              </Button>
            )}
          </div>
        )}
      </div>
    </header>
  )
}
