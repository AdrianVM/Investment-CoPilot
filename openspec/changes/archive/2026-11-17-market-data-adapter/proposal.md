# Proposal

## Why

The planned `strategy-assessment` change scores stocks against Peter Lynch's criteria (see `docs/peter-lynch-stock-criteria.jpeg`). Before it can do that, the app needs a way to get the numbers behind those criteria for a ticker symbol. This change adds that data behind a provider-agnostic adapter, so the choice of a real data vendor (with its costs, rate limits and licence terms) can be made later without changing the rest of the app.

## What Changes

- Add a stock fundamentals model to the MarketData module that covers every Lynch criterion: price, market capitalisation, trailing and forward P/E, EPS growth, institutional ownership, insider ownership and debt-to-equity, plus company name, currency, data source and an "as of" timestamp. Any metric may be missing, because real providers do not supply every metric for every stock.
- Add an internal provider adapter interface in the MarketData module. The provider is selected through settings only. Startup fails on an unknown provider name.
- Ship one provider: a deterministic **fake provider** with a small fixed set of fictional tickers chosen to cover edge cases (passes every criterion, fails several, missing metrics, non-USD currency). No real data vendor is integrated in this change.
- Cache fetched fundamentals in the `market_data` schema, shared by all users, with a configurable maximum age. If the provider fails, the last cached data is served and marked as stale.
- Expose the data in-process through `IMarketDataModule` (for the Assessment module) and over HTTP as `GET /api/market-data/stocks/{symbol}`, which requires authentication like every other API endpoint.
- Add a stock lookup to the frontend: the user enters a ticker and sees its fundamentals, with the source and as-of time, and missing metrics shown as "not available".
- Regenerate the TypeScript API client.

Out of scope: any real market data vendor (a later change adds one as another adapter); price history and charts; scoring stocks against the criteria (`strategy-assessment`); watchlists, search by company name, and portfolio tracking.

## Capabilities

### New Capabilities
- `market-data`: Looking up a stock's current fundamentals by ticker symbol: which metrics are provided and in what units, symbol validation, behaviour for unknown symbols, provider selection through settings, caching and stale-data fallback, and the stock lookup in the web app.

### Modified Capabilities
- None. The new endpoint follows the existing `user-identity` requirement that every API endpoint requires authentication, and market data is shared reference data, not user-owned, so the ownership requirements do not change.

## Impact

- Backend: new code in `backend/src/Modules/MarketData/` (fundamentals model, provider interface, fake provider, cache, endpoint) and `MarketData.Contracts` (`IMarketDataModule` methods and DTOs); a new EF Core migration for the cache table in the `market_data` schema; new settings section `MarketData`.
- Frontend: a new stock lookup component, and the regenerated client in `frontend/src/api/generated/`.
- `backend/openapi.json` gains the new endpoint.
- Tests: unit tests for symbol normalisation and the cache policy; integration tests for the endpoint; frontend component tests.
- No new external dependencies or accounts.
