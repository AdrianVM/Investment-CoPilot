import { screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApiClient } from '../api/apiClient'
import { client } from '../api/generated/client.gen'
import { renderWithQuery } from '../test/renderWithQuery'
import { ProfileCard } from './ProfileCard'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  fetchMock.mockReset()
  configureApiClient('http://api.test', async () => 'access-token-123')
  client.setConfig({ fetch: fetchMock })
})

function respondWith(status: number, body?: unknown) {
  fetchMock.mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  )
}

describe('ProfileCard', () => {
  it('shows the profile from /api/me', async () => {
    respondWith(200, { id: 'u1', email: 'alice@example.test', displayName: 'Alice Tester' })

    renderWithQuery(<ProfileCard />)

    expect(await screen.findByText('Alice Tester')).toBeInTheDocument()
    expect(screen.getByText('alice@example.test')).toBeInTheDocument()
  })

  it('sends the access token as a bearer token', async () => {
    respondWith(200, { id: 'u1', email: null, displayName: null })

    renderWithQuery(<ProfileCard />)
    await screen.findAllByText('Not provided')

    const request = fetchMock.mock.calls[0][0] as Request
    expect(request.url).toBe('http://api.test/api/me')
    expect(request.headers.get('Authorization')).toBe('Bearer access-token-123')
  })

  it('reports a failure to load', async () => {
    respondWith(401)

    renderWithQuery(<ProfileCard />)

    expect(await screen.findByRole('alert')).toHaveTextContent('could not be loaded')
  })
})
