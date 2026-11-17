import { useQuery } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { ApiStatusError } from '../api/apiStatusError'
import { LynchChecklist } from '../assessment/LynchChecklist'
import { getStockFundamentalsQueryKey } from '../api/generated/@tanstack/react-query.gen'
import { getStockFundamentals } from '../api/generated/sdk.gen'
import type { StockFundamentals } from '../api/generated/types.gen'
import { Button } from '../ui/Button'
import { FigureSheet, type FigureRow } from '../ui/FigureSheet'
import { Notice } from '../ui/Notice'
import { TextField } from '../ui/TextField'
import {
  formatCompactMoney,
  formatDateTime,
  formatMoney,
  formatPercent,
  formatRatio,
  normaliseSymbol,
} from './format'

async function fetchStock(symbol: string, signal: AbortSignal) {
  const { data, response } = await getStockFundamentals({ path: { symbol }, signal })
  if (!data) {
    throw new ApiStatusError(response?.status ?? 0, 'Stock lookup')
  }
  return data
}

/** Lets the user enter a ticker symbol and shows the stock's fundamentals from GET /api/market-data/stocks/{symbol}. */
export function StockLookup() {
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
    <section className="stock-lookup" aria-labelledby="stock-lookup-heading">
      <h2 id="stock-lookup-heading">Look up a stock</h2>
      <form onSubmit={submit} className="stock-lookup__form">
        <TextField
          label="Ticker symbol"
          hint="For example LYNCH, or SAP.DE for an exchange suffix."
          value={input}
          onChange={(event) => setInput(event.target.value)}
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          trailing={
            <Button type="submit" variant="primary">
              Look up
            </Button>
          }
        />
      </form>

      {invalidInput !== null ? (
        <InvalidSymbol input={invalidInput} />
      ) : symbol === null ? null : query.isPending ? (
        <p className="stock-lookup__status">Looking up {symbol}…</p>
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
    <Notice tone="danger" lead="Check the symbol:">
      {input === '' ? 'Enter a ticker symbol.' : `“${input}” is not a valid ticker symbol.`} A symbol has 1 to 15
      letters, digits, “.” or “-”.
    </Notice>
  )
}

function LookupFailed({ symbol, error }: { symbol: string; error: Error }) {
  const status = error instanceof ApiStatusError ? error.status : 0
  if (status === 400) {
    return <InvalidSymbol input={symbol} />
  }
  if (status === 404) {
    return (
      <Notice tone="info" lead="Not found:" announce>
        No stock found for “{symbol}”.
      </Notice>
    )
  }
  return <Notice tone="danger">Market data could not be loaded. Try again later.</Notice>
}

const notAvailable = 'Not available'

function row(label: string, value: string | null): FigureRow {
  return value === null ? { label, value: notAvailable, missing: true } : { label, value }
}

function map<T>(value: T | null, format: (value: T) => string) {
  return value === null ? null : format(value)
}

function StockDetails({ stock }: { stock: StockFundamentals }) {
  const asOf = formatDateTime(stock.asOf)
  return (
    <div className="stock-lookup__result">
      {stock.isStale && (
        <Notice tone="warning">This data could not be refreshed. It is from {asOf}.</Notice>
      )}
      <FigureSheet
        title={stock.companyName}
        aside={stock.symbol}
        rows={[
          row('Price', map(stock.price, formatMoney)),
          row('Market capitalisation', map(stock.marketCap, formatCompactMoney)),
          row('Trailing P/E', map(stock.trailingPe, formatRatio)),
          row('Forward P/E', map(stock.forwardPe, formatRatio)),
          row('EPS growth (year over year)', map(stock.epsGrowth, formatPercent)),
          row('Institutional ownership', map(stock.institutionalOwnership, formatPercent)),
          row('Insider ownership', map(stock.insiderOwnership, formatPercent)),
          row('Debt to equity', map(stock.debtToEquity, formatRatio)),
        ]}
        footer={
          <p>
            {stock.source}, as of {asOf}. Prices in {stock.currency}.
          </p>
        }
      />
      <LynchChecklist symbol={stock.symbol} />
    </div>
  )
}
