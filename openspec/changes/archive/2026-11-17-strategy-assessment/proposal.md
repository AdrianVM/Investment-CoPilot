# Proposal

## Why

The app can show a stock's figures, but the user still has to compare each one with Peter Lynch's rules of thumb (`docs/peter-lynch-stock-criteria.jpeg`) in their head. This change does that comparison: it checks a stock against the six criteria and shows which it meets, which it fails, and which cannot be checked because data is missing. The market cap rule is set in US dollars, so stocks listed in other currencies also need exchange rates to be assessed fairly.

## What Changes

- Add a **Peter Lynch assessment** in the Assessment module, computed on demand from the MarketData module's fundamentals. It has six criteria with the thresholds from the reference image:
  1. P/E: trailing P/E below 25 **and** forward P/E below 15
  2. Institutional ownership below 10 %
  3. Insider ownership above 20 %
  4. EPS growth above 15 %
  5. Debt-to-equity below 35 %
  6. Market capitalisation below US$5 billion
- Each criterion gets a verdict, **meets**, **does not meet** or **cannot be assessed**, together with the stock's actual value(s) and the threshold. The assessment also gives a summary count ("Meets 4 of 6 criteria; 1 could not be assessed"). There is no blended score.
- The strategy is modelled as a list of criteria behind a strategy identifier, so other strategies can be added later without changing the API shape. Users cannot configure thresholds.
- Add **exchange rates** to the MarketData module: convert an amount between currencies through a provider-agnostic rate provider, selected by settings. Only a fake provider with fixed sample rates for a handful of major currencies exists for now. The market cap criterion converts non-USD market caps to USD and shows the rate it used.
- Expose the assessment over HTTP as `GET /api/assessments/{strategy}/stocks/{symbol}` (strategy `peter-lynch`), and show it in the web app beneath the stock's figures, with each verdict marked by text and icon as well as colour.
- Show that the assessment is informational and not investment advice.

Out of scope: saving assessments, history and watchlists; user-adjustable thresholds; other strategies; a blended score; real exchange-rate or market data vendors.

## Capabilities

### New Capabilities
- `strategy-assessment`: Assessing a stock against a named investment strategy (Peter Lynch's six criteria for now): the criteria and thresholds, the verdict rules including missing data and currency conversion, the summary count, the HTTP endpoint and the assessment shown in the web app.

### Modified Capabilities
- `market-data`: Adds the requirement that amounts can be converted between currencies using exchange rates from a provider chosen through settings, with an as-of time and a source, and a clear outcome when a rate is not available. The existing stock fundamentals requirements are unchanged.

## Impact

- Backend:
  - `Assessment.Contracts` and `Assessment`: the strategy model, the Lynch criteria, the assessment service and the endpoint. The Assessment project gains a reference to `MarketData.Contracts`, which the module rules allow.
  - `MarketData.Contracts` and `MarketData`: the exchange-rate contract, the provider interface, a fake provider and new settings (`MarketData:ExchangeRateProvider`).
- No database changes: assessments are not stored, and fake rates are held in code.
- Frontend: a Lynch checklist component under the stock figure sheet; new pass/fail colour tokens, covered by the existing contrast test; the regenerated API client.
- Tests: unit tests for every criterion's verdict rules and the conversion; integration tests for the endpoint; component tests for the checklist.
