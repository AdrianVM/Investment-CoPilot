# Proposal

## Why

The web app still uses browser defaults: a plain header, unstyled buttons and inputs, and the Vite placeholder icon. Before more screens arrive with `strategy-assessment`, the app needs a consistent visual language, so new screens reuse it instead of each inventing its own. The goal is a calm, data-first look that suits a tool for reading financial figures.

## What Changes

- Add a design foundation: CSS design tokens (colour, typography, spacing, radii, borders) as custom properties, with a light and a dark theme chosen automatically from the operating system preference. There is no theme switch.
- Define a calm visual direction: neutral surfaces, a single accent colour, status colours used sparingly and never as the only signal, and clear typographic hierarchy. Numbers use tabular figures so digits line up.
- Add a small set of in-house UI components built on the tokens: button, text field, panel, notice (error, warning, info), and a label/value metric list.
- Restyle the existing screens with them: the app header (app mark, signed-in user's name, sign-out), the signed-out state (short explanation and a sign-in call to action), the profile card, and the stock lookup (form, result header, metric list, source line, stale and error notices).
- Replace the Vite placeholder favicon with an app mark.
- Set accessibility baselines for the whole app: WCAG 2.2 AA contrast in both themes, a visible keyboard focus indicator, reflow down to 320 px wide without horizontal scrolling, and reduced motion when the user asks for it.

No new dependencies: the styling is plain CSS plus React components, using the system font stack, and it works under the existing strict Content Security Policy.

Out of scope: navigation, routing and new pages (a later app-shell change); a manual theme switch; charts; and any change to what data is shown.

## Capabilities

### New Capabilities
- `visual-design`: How the web app looks and presents information across all screens: themes that follow the system preference, contrast and focus visibility, number presentation, reflow on narrow screens, reduced motion, status shown by more than colour, and the signed-out and signed-in header.

### Modified Capabilities
- None. The stock lookup keeps every behaviour in the `market-data` requirement "Users can look up a stock in the web app"; only its appearance changes.

## Impact

- Frontend only: `frontend/src/index.css` is replaced by token and base stylesheets; new components under `frontend/src/ui/`; `App.tsx`, `ProfileCard.tsx` and `StockLookup.tsx` are restyled; `frontend/public/favicon.svg` is replaced.
- Tests: existing component tests keep passing (they select by role and text, not by class); new tests for the components and for token contrast.
- No backend, API or generated-client changes.
