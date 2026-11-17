/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'

// Read from disk: Vitest replaces CSS imports (even ?raw) with empty modules.
const tokensCss = readFileSync(join(import.meta.dirname, 'tokens.css'), 'utf8')

// Spec visual-design: "Text and controls meet contrast minimums" (WCAG 2.2 AA), checked in both themes.

type Theme = 'light' | 'dark'

const tokens = new Map<string, Record<Theme, string>>()
for (const [, name, light, dark] of tokensCss.matchAll(/--([\w-]+):\s*light-dark\((#[0-9a-f]{6}),\s*(#[0-9a-f]{6})\)/gi)) {
  tokens.set(name, { light, dark })
}

function luminance(hex: string) {
  const [r, g, b] = [1, 3, 5].map((i) => {
    const c = parseInt(hex.slice(i, i + 2), 16) / 255
    return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

function contrast(foreground: string, background: string, theme: Theme) {
  const fg = tokens.get(foreground)?.[theme]
  const bg = tokens.get(background)?.[theme]
  if (!fg || !bg) throw new Error(`Unknown token pair ${foreground} on ${background}`)
  const [hi, lo] = [luminance(fg), luminance(bg)].sort((a, b) => b - a)
  return (hi + 0.05) / (lo + 0.05)
}

/** Text: 4.5:1. */
const textPairs = [
  ['color-text', 'color-paper'],
  ['color-text', 'color-surface'],
  ['color-text-muted', 'color-paper'],
  ['color-text-muted', 'color-surface'],
  ['color-ink', 'color-paper'], // links
  ['color-ink', 'color-surface'],
  ['color-on-ink', 'color-ink'], // primary button label
  ['color-on-ink', 'color-ink-hover'],
  ['color-text', 'color-warning-surface'],
  ['color-warning', 'color-warning-surface'], // notice lead
  ['color-text', 'color-danger-surface'],
  ['color-danger', 'color-danger-surface'],
  ['color-danger', 'color-paper'],
  ['color-text', 'color-info-surface'],
  ['color-info', 'color-info-surface'],
  ['color-pass', 'color-pass-surface'], // verdict label
  ['color-pass', 'color-surface'],
  ['color-text', 'color-pass-surface'],
  ['color-fail', 'color-fail-surface'],
  ['color-fail', 'color-surface'],
  ['color-text', 'color-fail-surface'],
] as const

/** Non-text: control boundaries, focus ring and status markers, 3:1. */
const uiPairs = [
  ['color-control-border', 'color-surface'],
  ['color-control-border', 'color-paper'],
  ['color-ink', 'color-paper'], // focus ring
  ['color-ink', 'color-surface'],
  ['color-warning', 'color-paper'], // notice border
  ['color-danger', 'color-paper'],
  ['color-info', 'color-paper'],
  ['color-pass', 'color-paper'], // verdict icon
  ['color-fail', 'color-paper'],
] as const

describe.each<Theme>(['light', 'dark'])('%s theme', (theme) => {
  it.each(textPairs)('text %s on %s has at least 4.5:1', (fg, bg) => {
    expect(contrast(fg, bg, theme)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(uiPairs)('%s against %s has at least 3:1', (fg, bg) => {
    expect(contrast(fg, bg, theme)).toBeGreaterThanOrEqual(3)
  })
})

it('every colour token defines both themes', () => {
  const colourDeclarations = tokensCss.match(/--color-[\w-]+:/g) ?? []
  expect(tokens.size).toBe(colourDeclarations.length)
})
