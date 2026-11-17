import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApiClient } from '../api/apiClient'
import { client } from '../api/generated/client.gen'
import type { StockFundamentals } from '../api/generated/types.gen'
import { renderWithQuery } from '../test/renderWithQuery'
import { formatCompactMoney, formatDateTime, formatMoney, formatPercent, formatRatio } from './format'
import { StockLookup } from './StockLookup'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  fetchMock.mockReset()
  configureApiClient('http://api.test', async () => 'access-token')
  client.setConfig({ fetch: fetchMock })
})

const lynch: StockFundamentals = {
  symbol: 'LYNCH',
  companyName: 'Lynchpin Tools Inc. (sample)',
  currency: 'USD',
  price: { amount: 42.5, currency: 'USD' },
  marketCap: { amount: 1_800_000_000, currency: 'USD' },
  trailingPe: 14.2,
  forwardPe: 11.8,
  epsGrowth: 0.21,
  institutionalOwnership: 0.07,
  insiderOwnership: 0.26,
  debtToEquity: 0.18,
  source: 'Sample data (fake provider)',
  asOf: '2026-11-17T09:30:00Z',
  isStale: false,
}

function respondWith(status: number, body?: unknown) {
  fetchMock.mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
    }),
  )
}

async function lookUp(symbol: string) {
  renderWithQuery(<StockLookup />)
  if (symbol !== '') {
    await userEvent.type(screen.getByLabelText('Ticker symbol'), symbol)
  }
  await userEvent.click(screen.getByRole('button', { name: 'Look up' }))
}

function metric(label: string) {
  return screen.getByText(label, { selector: 'dt' }).nextElementSibling?.textContent
}

describe('StockLookup', () => {
  it('requests nothing before a symbol is submitted', () => {
    renderWithQuery(<StockLookup />)

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('shows every metric with units, the source and the as-of time', async () => {
    respondWith(200, lynch)

    await lookUp(' lynch ')

    expect(await screen.findByRole('heading', { name: 'Lynchpin Tools Inc. (sample) (LYNCH)' })).toBeInTheDocument()
    expect(metric('Price')).toBe(formatMoney(lynch.price!))
    expect(metric('Market capitalisation')).toBe(formatCompactMoney(lynch.marketCap!))
    expect(metric('Trailing P/E')).toBe(formatRatio(14.2))
    expect(metric('Forward P/E')).toBe(formatRatio(11.8))
    expect(metric('EPS growth (year over year)')).toBe(formatPercent(0.21))
    expect(metric('Institutional ownership')).toBe(formatPercent(0.07))
    expect(metric('Insider ownership')).toBe(formatPercent(0.26))
    expect(metric('Debt to equity')).toBe(formatRatio(0.18))
    expect(screen.getByText(/Source: Sample data \(fake provider\)/)).toHaveTextContent(formatDateTime(lynch.asOf))
    expect(screen.queryByText(/could not be refreshed/)).not.toBeInTheDocument()

    const request = fetchMock.mock.calls[0][0] as Request
    expect(request.url).toBe('http://api.test/api/market-data/stocks/LYNCH')
    expect(request.headers.get('Authorization')).toBe('Bearer access-token')
  })

  it('shows "Not available" for missing metrics', async () => {
    respondWith(200, { ...lynch, forwardPe: null, institutionalOwnership: null, price: null })

    await lookUp('LYNCH')

    await screen.findByRole('heading', { name: /LYNCH/ })
    expect(metric('Forward P/E')).toBe('Not available')
    expect(metric('Institutional ownership')).toBe('Not available')
    expect(metric('Price')).toBe('Not available')
    expect(metric('Trailing P/E')).toBe(formatRatio(14.2))
  })

  it('notes when the data is stale', async () => {
    respondWith(200, { ...lynch, isStale: true })

    await lookUp('LYNCH')

    const note = await screen.findByText(/could not be refreshed/)
    expect(note).toHaveTextContent(formatDateTime(lynch.asOf))
  })

  it('reports an unknown symbol', async () => {
    respondWith(404)

    await lookUp('NOSUCH')

    expect(await screen.findByRole('alert')).toHaveTextContent('No stock found for “NOSUCH”.')
    expect(screen.queryByText('Price')).not.toBeInTheDocument()
  })

  it('reports a symbol the API rejects', async () => {
    respondWith(400, { title: 'Invalid ticker symbol', status: 400 })

    await lookUp('ABC')

    expect(await screen.findByRole('alert')).toHaveTextContent('“ABC” is not a valid ticker symbol.')
  })

  it('rejects an invalid symbol without calling the API', async () => {
    await lookUp('AB$C')

    expect(await screen.findByRole('alert')).toHaveTextContent('“AB$C” is not a valid ticker symbol.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('asks for a symbol when none was entered', async () => {
    await lookUp('')

    expect(await screen.findByRole('alert')).toHaveTextContent('Enter a ticker symbol.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('reports other failures', async () => {
    respondWith(503, { title: 'Market data unavailable', status: 503 })

    await lookUp('LYNCH')

    expect(await screen.findByRole('alert')).toHaveTextContent('Market data could not be loaded.')
  })
})
