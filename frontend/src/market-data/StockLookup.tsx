import { useQuery } from '@tanstack/react-query'
import { type FormEvent, useId, useState } from 'react'
import { getStockFundamentalsQueryKey } from '../api/generated/@tanstack/react-query.gen'
import { getStockFundamentals } from '../api/generated/sdk.gen'
import type { StockFundamentals } from '../api/generated/types.gen'
import {
  formatCompactMoney,
  formatDateTime,
  formatMoney,
  formatPercent,
  formatRatio,
  normaliseSymbol,
} from './format'

/** A failed lookup, with the HTTP status so the message (and retry policy) can depend on it. */
export class StockLookupError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Stock lookup failed with HTTP ${status}`)
    this.status = status
  }
}

async function fetchStock(symbol: string, signal: AbortSignal) {
  const { data, response } = await getStockFundamentals({ path: { symbol }, signal })
  if (!data) {
    throw new StockLookupError(response?.status ?? 0)
  }
  return data
}

/** Lets the user enter a ticker symbol and shows the stock's fundamentals from GET /api/market-data/stocks/{symbol}. */
export function StockLookup() {
  const inputId = useId()
  const [input, setInput] = useState('')
  const [symbol, setSymbol] = useState<string | null>(null)
  const [invalidInput, setInvalidInput] = useState<string | null>(null)

  const query = useQuery({
    queryKey: getStockFundamentalsQueryKey({ path: { symbol: symbol ?? '' } }),
    queryFn: ({ signal }) => fetchStock(symbol!, signal),
    enabled: symbol !== null,
  })

  function submit(event: FormEvent) {
    event.preventDefault()
    const normalised = normaliseSymbol(input)
    setInvalidInput(normalised ? null : input.trim())
    setSymbol(normalised)
  }

  return (
    <section aria-labelledby="stock-lookup-heading">
      <h2 id="stock-lookup-heading">Look up a stock</h2>
      <form onSubmit={submit} className="stock-lookup-form">
        <label htmlFor={inputId}>Ticker symbol</label>
        <input
          id={inputId}
          value={input}
          onChange={(event) => setInput(event.target.value)}
          autoComplete="off"
          spellCheck={false}
          placeholder="e.g. LYNCH"
        />
        <button type="submit">Look up</button>
      </form>

      {invalidInput !== null ? (
        <InvalidSymbol input={invalidInput} />
      ) : symbol === null ? null : query.isPending ? (
        <p>Looking up {symbol}…</p>
      ) : query.isError ? (
        <LookupFailed symbol={symbol} error={query.error} />
      ) : (
        <StockDetails stock={query.data} />
      )}
    </section>
  )
}

function InvalidSymbol({ input }: { input: string }) {
  return (
    <p role="alert">
      {input === '' ? 'Enter a ticker symbol.' : `“${input}” is not a valid ticker symbol.`} A symbol has 1 to 15
      letters, digits, “.” or “-”.
    </p>
  )
}

function LookupFailed({ symbol, error }: { symbol: string; error: Error }) {
  const status = error instanceof StockLookupError ? error.status : 0
  if (status === 400) {
    return <InvalidSymbol input={symbol} />
  }
  if (status === 404) {
    return <p role="alert">No stock found for “{symbol}”.</p>
  }
  return <p role="alert">Market data could not be loaded. Try again later.</p>
}

const notAvailable = 'Not available'

function StockDetails({ stock }: { stock: StockFundamentals }) {
  const metrics: [label: string, value: string][] = [
    ['Price', stock.price ? formatMoney(stock.price) : notAvailable],
    ['Market capitalisation', stock.marketCap ? formatCompactMoney(stock.marketCap) : notAvailable],
    ['Trailing P/E', stock.trailingPe !== null ? formatRatio(stock.trailingPe) : notAvailable],
    ['Forward P/E', stock.forwardPe !== null ? formatRatio(stock.forwardPe) : notAvailable],
    ['EPS growth (year over year)', stock.epsGrowth !== null ? formatPercent(stock.epsGrowth) : notAvailable],
    [
      'Institutional ownership',
      stock.institutionalOwnership !== null ? formatPercent(stock.institutionalOwnership) : notAvailable,
    ],
    ['Insider ownership', stock.insiderOwnership !== null ? formatPercent(stock.insiderOwnership) : notAvailable],
    ['Debt to equity', stock.debtToEquity !== null ? formatRatio(stock.debtToEquity) : notAvailable],
  ]

  return (
    <article aria-labelledby="stock-heading">
      <h3 id="stock-heading">
        {stock.companyName} ({stock.symbol})
      </h3>
      {stock.isStale && (
        <p className="stale-note">
          This data could not be refreshed and may be out of date. It is from {formatDateTime(stock.asOf)}.
        </p>
      )}
      <dl>
        {metrics.map(([label, value]) => (
          <div key={label} className="metric">
            <dt>{label}</dt>
            <dd>{value}</dd>
          </div>
        ))}
      </dl>
      <p className="stock-source">
        Source: {stock.source} · As of {formatDateTime(stock.asOf)} · Prices in {stock.currency}
      </p>
    </article>
  )
}
