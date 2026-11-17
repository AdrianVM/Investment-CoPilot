import type { Money } from '../api/generated/types.gen'

// Formatting for market data. `locale` defaults to the browser's; tests pass one explicitly.

export function formatMoney(value: Money, locale?: string) {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: value.currency }).format(value.amount)
}

/** Large amounts such as market capitalisation, e.g. "$1.8B". */
export function formatCompactMoney(value: Money, locale?: string) {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: value.currency,
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(value.amount)
}

/** A fraction as a percentage: 0.26 → "26%". */
export function formatPercent(fraction: number, locale?: string) {
  return new Intl.NumberFormat(locale, { style: 'percent', maximumFractionDigits: 1 }).format(fraction)
}

/** A plain ratio such as P/E, to two decimals. */
export function formatRatio(value: number, locale?: string) {
  return new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value)
}

/** A threshold as written in a rule: no trailing zeros, e.g. 25 or 0.35. */
export function formatThreshold(value: number, locale?: string) {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 2 }).format(value)
}

/** An exchange rate, with up to six decimals, e.g. 1.085. */
export function formatRate(value: number, locale?: string) {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 6 }).format(value)
}

export function formatDateTime(iso: string, locale?: string) {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso))
}

/** Same rule as the API: 1–15 letters, digits, '.' or '-', starting with a letter or digit. */
const symbolPattern = /^[A-Z0-9][A-Z0-9.-]{0,14}$/

/** Trims and upper-cases user input; returns null when it is not a valid ticker symbol. */
export function normaliseSymbol(input: string) {
  const symbol = input.trim().toUpperCase()
  return symbolPattern.test(symbol) ? symbol : null
}
