import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { AppHeader } from './AppHeader'

const noop = () => {}

describe('AppHeader', () => {
  it('shows the signed-in user next to "Sign out"', async () => {
    const onSignOut = vi.fn<() => void>()
    render(<AppHeader userName="Alice Tester" signedIn checking={false} onSignIn={noop} onSignOut={onSignOut} />)

    const banner = screen.getByRole('banner')
    expect(banner).toHaveTextContent('Investment Co-Pilot')
    const signOut = screen.getByRole('button', { name: 'Sign out' })
    expect(signOut.previousElementSibling).toHaveTextContent('Alice Tester')
    await userEvent.click(signOut)
    expect(onSignOut).toHaveBeenCalledOnce()
  })

  it('shows no name when signed out', () => {
    render(<AppHeader userName={null} signedIn={false} checking={false} onSignIn={noop} onSignOut={noop} />)

    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.getByRole('banner')).toHaveTextContent(/^Investment Co-PilotSign in$/)
  })

  it('offers no action while the sign-in state is being checked', () => {
    render(<AppHeader userName={null} signedIn={false} checking onSignIn={noop} onSignOut={noop} />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})
