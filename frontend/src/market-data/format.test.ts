import { describe, expect, it } from 'vitest'
import { formatCompactMoney, formatMoney, formatPercent, formatRatio, normaliseSymbol } from './format'

describe('formatting', () => {
  it('formats money in its own currency', () => {
    expect(formatMoney({ amount: 42.5, currency: 'USD' }, 'en-US')).toBe('$42.50')
    expect(formatMoney({ amount: 56.8, currency: 'EUR' }, 'en-US')).toBe('€56.80')
  })

  it('formats market capitalisation compactly', () => {
    expect(formatCompactMoney({ amount: 1_800_000_000, currency: 'USD' }, 'en-US')).toBe('$1.8B')
  })

  it('formats fractions as percentages', () => {
    expect(formatPercent(0.26, 'en-US')).toBe('26%')
    expect(formatPercent(0.155, 'en-US')).toBe('15.5%')
  })

  it('formats ratios to two decimals', () => {
    expect(formatRatio(14.2, 'en-US')).toBe('14.20')
  })
})

describe('normaliseSymbol', () => {
  it.each([
    [' lynch ', 'LYNCH'],
    ['sap.de', 'SAP.DE'],
    ['brk-b', 'BRK-B'],
  ])('accepts %j as %j', (input, expected) => {
    expect(normaliseSymbol(input)).toBe(expected)
  })

  it.each(['', '   ', 'AB$C', '.ABC', 'ABCDEFGHIJKLMNOP'])('rejects %j', (input) => {
    expect(normaliseSymbol(input)).toBeNull()
  })
})
