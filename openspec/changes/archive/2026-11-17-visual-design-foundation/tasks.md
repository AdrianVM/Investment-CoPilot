# Tasks

## 1. Tokens, font and base styles

- [x] 1.1 Add `frontend/src/styles/tokens.css` with the colour (`light-dark()`), type scale, spacing and radius tokens from the design, and `frontend/src/styles/tokens.test.ts`, which parses the file and asserts 4.5:1 for every text/background pair and 3:1 for control borders, focus ring and status markers in both themes; tune token values until the test passes
- [x] 1.2 Download Atkinson Hyperlegible Next (variable WOFF2) from its official source into `frontend/public/fonts/` with its OFL licence file, and confirm the `tnum` feature exists (for example with `fonttools`); if it is missing, use Public Sans instead and update the design's Typography section; verify the file loads from `/fonts/` in `npm run dev`
- [x] 1.3 Add `frontend/src/styles/base.css` (light fallbacks before tokens, reset, element defaults, `@font-face` with `font-display: swap`, `:focus-visible` ring, `prefers-reduced-motion` rule, `.figure` with tabular numerals), import both stylesheets from `main.tsx` in place of `index.css`, add a font preload link to `index.html`, delete `index.css`, and verify `npm run build:local` succeeds and the existing tests pass

## 2. Components

- [x] 2.1 Add `Button`, `TextField` and `Notice` in `frontend/src/ui/` with their styles in `ui.css`, and verify with Vitest tests that the button variants render native buttons, the text field's label is associated with its input (found by `getByLabelText`), and each notice tone shows its text lead ("Out of date:", "Error:")
- [x] 2.2 Add `FigureSheet` (title, aside, label/value rows as a `<dl>`, footer; one column below 40rem, two from 40rem) and verify with a test that each value is the `<dd>` following its label's `<dt>` and carries the `.figure` class
- [x] 2.3 Add the app mark as an inline SVG component using `currentColor`, replace `frontend/public/favicon.svg` with the same mark (with a `prefers-color-scheme` style for dark tabs), add `AppHeader` (mark, app name, user's display name or email when signed in, sign-in/sign-out button), and verify with tests that the signed-in header shows the name next to "Sign out" and the signed-out header shows no name

## 3. Restyle the screens

- [x] 3.1 Use `AppHeader` in `App.tsx`, add the signed-out description and prominent "Sign in" button, show sign-in errors in a danger `Notice`, and order the signed-in page as stock lookup then profile; update `App.test.tsx` for the new signed-out copy and header name, and verify all `App` tests pass
- [x] 3.2 Rebuild `StockLookup` markup with `TextField`, `Button`, `Notice` (warning for stale data, danger for errors, info for "not found") and `FigureSheet`, with provenance written as a sentence; keep every existing `StockLookup` test passing with at most changes to expected copy, and add an assertion that the stale note starts with "Out of date:"
- [x] 3.3 Restyle `ProfileCard` as the quiet one-line profile section, and verify its existing tests still pass

## 4. Verification

- [x] 4.1 Run `npm run lint`, `npm run typecheck`, `npm test` and `npm run build:local` in `frontend`, and verify all pass
- [x] 4.2 Serve the build with `npm run preview` against the local environment and check by hand in both themes (switching the system or devtools colour scheme while the page is open): no CSP errors in the console, the font loads, Tab shows a focus ring on every control, a 320 px viewport and 200 % zoom need no horizontal scrolling, reduced-motion emulation removes transitions, and the signed-out page, `LYNCH`, `SPARSE`, `EUROCO` and an unknown symbol render as designed (the fake provider never fails, so the stale notice is covered by the component test in 3.2 rather than checked here)
