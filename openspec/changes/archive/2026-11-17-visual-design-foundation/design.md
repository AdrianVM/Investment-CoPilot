# Design

## Context

All styling is one 60-line `frontend/src/index.css` with browser-default controls. `App.tsx` renders the header, an error line and either a sign-in prompt or `ProfileCard` + `StockLookup`. The built app carries a strict Content Security Policy (`frontend/csp.ts`): `style-src 'self'`, `font-src 'self'`, `img-src 'self' data:`, so styles, fonts and icons must ship with the app. Component tests select by role and text, not by class. See proposal.md for motivation and `specs/visual-design/spec.md` for the required behaviour.

## Goals / Non-Goals

**Goals:**
- One token file that every later screen uses, so `strategy-assessment` adds no new colours or sizes of its own.
- A recognisable but quiet identity: the figures are the one memorable element; everything around them stays plain.

**Non-Goals:**
- A general component library. Only the components the current screens need are built; more are added when a screen needs them.
- Visual regression testing tooling. Appearance is checked by hand in both themes; contrast is checked by a test.

## Decisions

### Visual direction: "the annotated figure sheet"

The app is a reading tool for company figures, in the spirit of Peter Lynch's "fourth-grade maths". The identity borrows from an investor annotating an annual report with a fountain pen: cool paper, graphite text, and one blue-black ink accent used only for actions and focus.

- **The bold element** is the stock's figure sheet: labels left, figures right-aligned in tabular numerals at a larger size than body text, grouped by spacing rather than boxes.
- **Everything else is quiet:** no shadows, no gradients, no card per section. Sections are separated by space and, where a boundary helps, one hairline rule. Corner radius only on controls (4 px) and the figure sheet surface (6 px), so radius signals "you can act on this" or "this is the result".
- **No accent green or red.** Green and red are reserved for future pass/fail signals in the assessment, so the brand accent is ink blue.
- **Copy:** sentence case, no all-caps labels, no eyebrow labels above headings, and provenance written as a sentence ("Sample data (fake provider), as of 17 Nov 2026, 09:30. Prices in USD.") rather than a dot-joined meta string.

### Colour tokens

Defined once in `frontend/src/styles/tokens.css` as custom properties whose values use `light-dark(<light>, <dark>)`, with `color-scheme: light dark` on `:root`. The browser picks the theme from the system preference and switches live (spec: Theme follows the system preference), and native controls and scrollbars follow it. Alternative considered: duplicated token blocks in a `prefers-color-scheme: dark` media query. Rejected because every token would be defined in two places; `light-dark()` is supported by all evergreen browsers.

| Token | Role | Light | Dark |
|---|---|---|---|
| `--color-paper` | Page background | `#F5F6F8` | `#13161D` |
| `--color-surface` | Figure sheet, inputs | `#FFFFFF` | `#1B1F28` |
| `--color-text` | Body text, figures | `#1B1F27` | `#E7E9EE` |
| `--color-text-muted` | Labels, provenance | `#59606E` | `#A3AAB7` |
| `--color-rule` | Hairline dividers (decorative) | `#D6DAE1` | `#2D333E` |
| `--color-control-border` | Input and secondary button borders | `#7D8697` | `#747D8E` |
| `--color-ink` | Accent: primary buttons, links, focus ring | `#2E3D9A` | `#A3B0F5` |
| `--color-on-ink` | Text on ink | `#FFFFFF` | `#13161D` |
| `--color-warning` / `--color-warning-surface` | Stale note | `#7A4A00` / `#FCF1DC` | `#F0B65A` / `#2B2314` |
| `--color-danger` / `--color-danger-surface` | Errors | `#A8261D` / `#FBE9E7` | `#F29890` / `#2E1A19` |

Values are starting points: a unit test (`tokens.test.ts`) parses `tokens.css` and asserts the spec's ratios for every text/background and control/background pair in both themes (4.5:1 text, 3:1 control borders, focus ring and status markers). A value that fails is adjusted until the test passes; the test, not this table, is authoritative.

### Typography

One family: **Atkinson Hyperlegible Next**, a SIL Open Font License typeface designed by the Braille Institute to keep look-alike characters distinct (0/O, 1/l/I, 5/S, 8/B), which matters when reading figures such as `0.18` or `1.8B`. It is self-hosted as one variable WOFF2 file in `frontend/public/fonts/` with its licence alongside, loaded with `@font-face` and `font-display: swap`, so it works under `font-src 'self'` and adds no package dependency. Fallback is the system UI stack. The implementation task first confirms the file has the OpenType `tnum` (tabular figures) feature; if it does not, the fallback choice is **Public Sans** (OFL, has `tnum`), under the same rules.

Alternative considered: the system font stack only. Rejected because it renders as a different face on every OS and makes figures look generic; one self-hosted file (~100 KB) is a fair price for the app's one typographic decision.

Scale (rem, 1 rem = 16 px), modular ratio about 1.2: `--text-sm` 0.875, `--text-base` 1, `--text-lg` 1.2, `--text-xl` 1.44, `--text-2xl` 1.75. Body line height 1.5, headings 1.2. Weights: 400 body, 600 headings and labels, 500 figures. All figures use `font-variant-numeric: tabular-nums` through the `.figure` utility (spec: Numbers line up). Text blocks are capped at 68ch.

### Spacing, layout and breakpoints

Spacing tokens on a 4 px base: `--space-1` 0.25rem … `--space-8` 3rem. Content is left-aligned in a single column, max width 60rem, with page padding `--space-4` (narrow) or `--space-6` (≥ 40rem). The figure sheet lays its metrics out in one column below 40rem and two columns (label/figure pairs side by side) from 40rem; everything wraps by intrinsic sizing, never fixed widths, so 320 px and 200 % text reflow (spec: Layout reflows on narrow screens).

```
┌──────────────────────────────────────────────────────────┐
│ [mark] Investment Co-Pilot             Alice Tester  [Sign out] │
├──────────────────────────────────────────────────────────┤
│ Look up a stock                                          │
│ Ticker symbol                                            │
│ [ LYNCH              ]  [Look up]                        │
│                                                          │
│ ╭ Lynchpin Tools Inc. (sample)                 LYNCH ╮   │
│ │ Price                $42.50   Trailing P/E   14.20 │   │
│ │ Market cap            $1.8B   Forward P/E    11.80 │   │
│ │ EPS growth              21%   Debt to equity  0.18 │   │
│ │ Institutional own.       7%   Insider own.     26% │   │
│ ╰ Sample data (fake provider), as of 17 Nov 2026 …   ╯   │
│                                                          │
│ Your profile   Alice Tester, alice@example.com           │
└──────────────────────────────────────────────────────────┘
```

The stock lookup moves above the profile, because looking up stocks is the task; the profile becomes a quiet one-line section at the end.

### Focus, motion and status

- Focus: `:focus-visible` gets a 2 px `--color-ink` outline with 2 px offset on every interactive element, defined once in the base stylesheet (spec: Keyboard focus is always visible). Default outlines are never removed without this replacement.
- Motion: only colour changes on hover and focus, with a 120 ms transition removed entirely under `prefers-reduced-motion: reduce` (spec: Motion respects the user's preference). No entrance animations.
- Status: notices carry a text lead ("Out of date:", "Error:") and a left border in the status colour; the colour is never the only signal (spec: Status is not shown by colour alone). Icons are inline SVG components with `aria-hidden` next to that text, not image files. Errors are announced to screen readers (`role="alert"`); other notices are announced only when they answer the user's own action (an `announce` option), such as a lookup that found nothing. In the stock lookup, an invalid symbol and a failed request are danger notices, "not found" is an info notice, and stale data is a warning that is not announced because it accompanies the result.

### Stylesheets and components

```
frontend/src/styles/
  tokens.css      # custom properties only
  base.css        # reset, element defaults, @font-face, focus, reduced motion, .figure
frontend/src/ui/
  Button.tsx      # variant: primary (ink) | secondary (outlined)
  TextField.tsx   # label, input, optional hint, optional trailing element (e.g. the submit
                  # button, which wraps below the input on narrow screens); forwards input props
  Notice.tsx      # tone: info | warning | danger; text lead + body
  FigureSheet.tsx # title, aside (symbol), rows of {label, value}, footer
  AppHeader.tsx   # mark, app name, user name, sign-in/out action
  ui.css          # component styles, one class prefix per component (.button, .text-field, …)
```

Plain CSS files imported once from `main.tsx`; class names are flat and single-class (`.button`, `.button--primary`) so selectors never compete on specificity. No CSS modules, CSS-in-JS or `style` props: CSS-in-JS would need inline `<style>` elements the CSP blocks, and `style` props are avoided by convention so nothing depends on how the CSP treats them (spec: Styling works under the Content Security Policy). Alternative considered: CSS modules. Rejected as unnecessary for a handful of prefixed components; can be adopted later without changing tokens.

`StockLookup` keeps its logic and test IDs (roles and text) and swaps its markup for `TextField`, `Button`, `Notice` and `FigureSheet`. `FigureSheet` renders a `<dl>` so labels and values stay associated for screen readers.

### App mark and favicon

A simple SVG mark, a ruled sheet with a check-like pen stroke in `--color-ink`, used inline in the header (as a React component, so it follows the theme through `currentColor`) and as `public/favicon.svg` (with an embedded `prefers-color-scheme` style so it stays visible on dark browser tabs). It replaces the Vite placeholder.

## Risks / Trade-offs

- [The chosen typeface lacks tabular figures or its licence terms differ from expected] → The font task verifies both before use, with Public Sans as the named fallback.
- [`light-dark()` is unsupported in an old browser] → Such browsers ignore the declarations; `base.css` sets plain light fallbacks before the tokens so the app stays readable in light colours.
- [Token values fail contrast in one theme] → `tokens.test.ts` fails the build; values are tuned until it passes.
- [Hand-checked appearance can regress unnoticed] → Accepted for now; screens are few. Revisit visual regression testing when the app shell adds pages.
- [A web font delays first text paint] → `font-display: swap` shows the fallback immediately; one preloaded WOFF2 file keeps the swap short.

## Open Questions

- The exact copy for the signed-out description. It is written during implementation, describing only what the app does today (stock look-up), and can be changed without affecting the specs or tasks.
