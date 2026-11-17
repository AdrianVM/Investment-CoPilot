import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { configureApiClient } from '../api/apiClient'
import { client } from '../api/generated/client.gen'
import type { StockAssessment } from '../api/generated/types.gen'
import { formatCompactMoney, formatDateTime, formatPercent, formatRate, formatRatio } from '../market-data/format'
import { renderWithQuery } from '../test/renderWithQuery'
import { LynchChecklist } from './LynchChecklist'
import { lynchAssessment } from './testData'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  fetchMock.mockReset()
  configureApiClient('http://api.test', async () => 'access-token')
  client.setConfig({ fetch: fetchMock })
})

function respondWith(status: number, body: unknown) {
  fetchMock.mockImplementation(async () =>
    Response.json(body, { status, headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' } }),
  )
}

async function renderChecklist(assessment: StockAssessment) {
  respondWith(200, assessment)
  renderWithQuery(<LynchChecklist symbol={assessment.symbol} />)
  return screen.findByRole('region', { name: 'Peter Lynch checklist' })
}

function item(name: string) {
  return screen.getByText(name, { selector: '.checklist__name' }).closest('li')!
}

describe('LynchChecklist', () => {
  it('shows the summary and every criterion as met, with values and thresholds', async () => {
    const checklist = await renderChecklist(lynchAssessment)

    expect(checklist).toHaveTextContent('Meets 6 of 6 criteria.')
    expect(within(checklist).getAllByRole('listitem')).toHaveLength(6)
    expect(item('P/E')).toHaveTextContent(
      `Meets: P/E Trailing P/E ${formatRatio(14.2)} (below 25), Forward P/E ${formatRatio(11.8)} (below 15)`,
    )
    expect(item('Insider ownership')).toHaveTextContent(`${formatPercent(0.26)} (above ${formatPercent(0.2)})`)
    expect(item('Debt to equity')).toHaveTextContent(`${formatRatio(0.18)} (below 0.35)`)
    expect(item('Market capitalisation')).toHaveTextContent(
      `${formatCompactMoney({ amount: 1_800_000_000, currency: 'USD' })} (below ${formatCompactMoney({ amount: 5_000_000_000, currency: 'USD' })})`,
    )
    expect(checklist).toHaveTextContent('For information only, not investment advice.')
    expect(checklist).toHaveTextContent(formatDateTime(lynchAssessment.asOf))

    const request = fetchMock.mock.calls[0][0] as Request
    expect(request.url).toBe('http://api.test/api/assessments/peter-lynch/stocks/LYNCH')
  })

  it('reads the verdict as text before the criterion, not by colour alone', async () => {
    await renderChecklist(lynchAssessment)

    const verdict = within(item('P/E')).getByText('Meets')
    expect(verdict).toHaveClass('checklist__verdict')
    expect(verdict.querySelector('svg')).toHaveAttribute('aria-hidden', 'true')
  })

  it('shows missing data as "Cannot be assessed" with the reason, and counts it in the summary', async () => {
    const sparse: StockAssessment = {
      ...lynchAssessment,
      criteria: lynchAssessment.criteria.map((c) =>
        c.id === 'institutionalOwnership'
          ? { ...c, verdict: 'cannotAssess', reason: 'dataNotAvailable', measures: c.measures.map((m) => ({ ...m, value: null })) }
          : c.id === 'debtToEquity'
            ? { ...c, verdict: 'doesNotMeet', measures: c.measures.map((m) => ({ ...m, value: 1.45 })) }
            : c,
      ),
      summary: { met: 4, notMet: 1, cannotAssess: 1, total: 6 },
    }

    const checklist = await renderChecklist(sparse)

    expect(checklist).toHaveTextContent('Meets 4 of 6 criteria. 1 could not be assessed.')
    expect(item('Institutional ownership')).toHaveTextContent('Cannot be assessed: Institutional ownership Data not available')
    expect(item('Debt to equity')).toHaveTextContent(`Does not meet: Debt to equity ${formatRatio(1.45)} (below 0.35)`)
  })

  it('lists the known value when only part of a criterion is missing', async () => {
    const partial: StockAssessment = {
      ...lynchAssessment,
      criteria: lynchAssessment.criteria.map((c) =>
        c.id === 'pe'
          ? {
              ...c,
              verdict: 'cannotAssess',
              reason: 'dataNotAvailable',
              measures: c.measures.map((m) => (m.id === 'forwardPe' ? { ...m, value: null } : { ...m, value: 19.7 })),
            }
          : c,
      ),
    }

    await renderChecklist(partial)

    expect(item('P/E')).toHaveTextContent(
      `Cannot be assessed: P/E Trailing P/E ${formatRatio(19.7)} (below 25), Forward P/E not available (below 15)`,
    )
  })

  it('shows a converted market cap with the rate and its source', async () => {
    const euroco: StockAssessment = {
      ...lynchAssessment,
      symbol: 'EUROCO',
      criteria: lynchAssessment.criteria.map((c) =>
        c.id === 'marketCap'
          ? {
              ...c,
              measures: c.measures.map((m) => ({ ...m, value: 3_472_000_000 })),
              conversion: {
                original: { amount: 3_200_000_000, currency: 'EUR' },
                converted: { amount: 3_472_000_000, currency: 'USD' },
                rate: 1.085,
                source: 'Sample rates (fake provider)',
                asOf: lynchAssessment.asOf,
              },
            }
          : c,
      ),
    }

    await renderChecklist(euroco)

    const marketCap = item('Market capitalisation')
    expect(marketCap).toHaveTextContent(formatCompactMoney({ amount: 3_200_000_000, currency: 'EUR' }))
    expect(marketCap).toHaveTextContent(`about ${formatCompactMoney({ amount: 3_472_000_000, currency: 'USD' })}`)
    expect(marketCap).toHaveTextContent(`Converted at 1 EUR = ${formatRate(1.085)} USD (Sample rates (fake provider)`)
  })

  it('explains a market cap that could not be converted', async () => {
    const noRate: StockAssessment = {
      ...lynchAssessment,
      criteria: lynchAssessment.criteria.map((c) =>
        c.id === 'marketCap' ? { ...c, verdict: 'cannotAssess', reason: 'exchangeRateNotAvailable' } : c,
      ),
    }

    await renderChecklist(noRate)

    expect(item('Market capitalisation')).toHaveTextContent('No exchange rate available to convert to US dollars')
  })

  it('reports a failure without hiding anything else', async () => {
    respondWith(503, { title: 'Market data unavailable', status: 503 })

    renderWithQuery(<LynchChecklist symbol="LYNCH" />)

    expect(await screen.findByRole('alert')).toHaveTextContent('The Peter Lynch checklist could not be loaded.')
  })
})
