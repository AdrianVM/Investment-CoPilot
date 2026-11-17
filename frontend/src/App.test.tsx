import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { configureApiClient } from './api/apiClient'
import { client } from './api/generated/client.gen'
import { AuthProvider } from './auth/AuthProvider'
import { createFakeUserManager, fakeUser } from './test/fakeUserManager'
import { renderWithQuery } from './test/renderWithQuery'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  configureApiClient('http://api.test', async () => 'access-token')
  client.setConfig({ fetch: fetchMock })
  fetchMock.mockImplementation(async () =>
    Response.json({ id: 'u1', email: 'alice@example.test', displayName: 'Alice Tester' }),
  )
})

afterEach(() => {
  window.history.replaceState(null, '', '/')
})

function renderApp(userManager: ReturnType<typeof createFakeUserManager>['userManager']) {
  return renderWithQuery(
    <AuthProvider userManager={userManager}>
      <App />
    </AuthProvider>,
  )
}

describe('signed out', () => {
  it('offers sign-in and shows no profile', async () => {
    const { userManager } = createFakeUserManager()

    renderApp(userManager)

    expect(await within(await screen.findByRole('banner')).findByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Read a company’s key figures in one place' })).toBeInTheDocument()
    expect(within(screen.getByRole('main')).getByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Your profile' })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Look up a stock' })).not.toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('redirects to the provider when the user chooses to sign in', async () => {
    const { fake, userManager } = createFakeUserManager()
    renderApp(userManager)

    await userEvent.click(await within(await screen.findByRole('banner')).findByRole('button', { name: 'Sign in' }))

    expect(fake.signinRedirect).toHaveBeenCalledOnce()
  })
})

describe('signed in', () => {
  it('shows the profile and offers sign-out', async () => {
    const { userManager } = createFakeUserManager(fakeUser())

    renderApp(userManager)

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(await screen.findByText('Alice Tester')).toBeInTheDocument()
  })

  it('shows the user\'s name from the sign-in token in the header', async () => {
    const { userManager } = createFakeUserManager(
      fakeUser({ profile: { sub: 'sub-1', iss: 'https://issuer.test', aud: 'web', exp: 0, iat: 0, name: 'Alice T.' } }),
    )

    renderApp(userManager)

    const banner = await screen.findByRole('banner')
    expect(await within(banner).findByText('Alice T.')).toBeInTheDocument()
  })

  it('offers the stock lookup', async () => {
    const { userManager } = createFakeUserManager(fakeUser())

    renderApp(userManager)

    expect(await screen.findByRole('heading', { name: 'Look up a stock' })).toBeInTheDocument()
  })

  it('is restored by a silent sign-in after a page reload', async () => {
    const { fake, userManager } = createFakeUserManager()
    fake.signinSilent.mockResolvedValue(fakeUser())

    renderApp(userManager)

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
  })

  it('completes the sign-in redirect and returns to the page the user started on', async () => {
    window.history.replaceState(null, '', '/callback?code=abc&state=xyz')
    const { fake, userManager } = createFakeUserManager()
    fake.signinRedirectCallback.mockResolvedValue(fakeUser({ state: { returnTo: '/portfolio?tab=1' } }))

    renderApp(userManager)

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(window.location.pathname + window.location.search).toBe('/portfolio?tab=1')
  })

  it('never returns to another site after sign-in', async () => {
    window.history.replaceState(null, '', '/callback?code=abc&state=xyz')
    const { fake, userManager } = createFakeUserManager()
    fake.signinRedirectCallback.mockResolvedValue(fakeUser({ state: { returnTo: '//evil.example' } }))

    renderApp(userManager)

    await screen.findByRole('button', { name: 'Sign out' })
    expect(window.location.pathname).toBe('/')
  })
})

describe('signed out again', () => {
  it('discards the tokens and shows the signed-out state', async () => {
    const { fake, userManager } = createFakeUserManager(fakeUser())
    renderApp(userManager)

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await within(await screen.findByRole('banner')).findByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(screen.queryByText('Alice Tester')).not.toBeInTheDocument()
    expect(fake.removeUser).toHaveBeenCalledOnce()
    expect(fake.signoutRedirect).toHaveBeenCalledExactlyOnceWith({ id_token_hint: 'id-token' })
    await waitFor(async () => expect(await fake.getUser()).toBeNull())
  })

  it('still signs out locally when the provider cannot be reached', async () => {
    const { fake, userManager } = createFakeUserManager(fakeUser())
    fake.signoutRedirect.mockRejectedValue(new Error('network'))
    renderApp(userManager)

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await within(await screen.findByRole('banner')).findByRole('button', { name: 'Sign in' })).toBeInTheDocument()
  })
})
