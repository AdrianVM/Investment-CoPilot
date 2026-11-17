# Tasks

## 1. Contracts and symbol parsing

- [x] 1.1 Add `StockFundamentals` and `StockLookupResult` (`Found`, `InvalidSymbol`, `NotFound`, `Unavailable`) to `MarketData.Contracts`, with XML docs stating each metric's unit and the EPS growth definition, and add `GetStockFundamentalsAsync` to `IMarketDataModule`; verify `dotnet build backend` succeeds and the architecture tests still pass
- [x] 1.2 Implement the internal `TickerSymbol` value type (trim, upper-case, `^[A-Z0-9][A-Z0-9.-]{0,14}$`), and verify with unit tests covering lower case, surrounding whitespace, exchange suffixes such as `SAP.DE`, the 15-character limit, a leading `.` or `-`, and invalid characters such as `AB$C`

## 2. Provider adapter and fake provider

- [x] 2.1 Add `IMarketDataProvider`, `ProviderFundamentals`, `MarketDataProviderException` and `MarketDataOptions` (`MarketData:Provider`, `MarketData:CacheMaxAge` defaulting to 24 hours) with startup validation that fails, naming the setting and the valid providers, when the provider name is unknown; set `"MarketData": { "Provider": "Fake" }` in `backend/src/Host/appsettings.json`; verify with an integration test that the host fails to start with an unknown provider name
- [x] 2.2 Implement `FakeMarketDataProvider` with the `LYNCH`, `BIGCO`, `SPARSE`, `LOSSCO` and `EUROCO` sample companies from the design (percentages as fractions, `LOSSCO` with null P/E values, `EUROCO` in EUR), and verify with unit tests that each symbol returns the same values on repeated calls, unknown symbols return null, and `LYNCH` meets and `BIGCO` fails the thresholds in `docs/peter-lynch-stock-criteria.jpeg`

## 3. Cache and lookup service

- [x] 3.1 Add the `CachedStockFundamentals` entity mapped to `market_data.stock_fundamentals` (symbol primary key, `Price` and `MarketCap` through `MoneyMapping`, `source`, `as_of`, `fetched_at`) and a new migration; verify `dotnet ef migrations list` for `MarketDataDbContext` shows it and an integration test finds the table after startup
- [x] 3.2 Implement `StockLookupService` (parse → fresh cache hit → provider fetch and upsert → stale fallback or `Unavailable` on provider failure, with the failure logged) using `TimeProvider`, and wire it into `MarketDataModuleFacade`; verify with integration tests against Testcontainers Postgres, using a counting/failing test provider and a fake `TimeProvider`, that fresh data is not re-fetched, expired data is re-fetched, a provider failure returns cached data with `IsStale` and its original as-of time, a failure with no cache returns `Unavailable`, an invalid symbol never reaches the provider, and two concurrent first lookups of one symbol both succeed and leave one row

## 4. HTTP endpoint

- [x] 4.1 Add `MarketDataEndpoints` (`GET /api/market-data/stocks/{symbol}`, operation `GetStockFundamentals`, tag `MarketData`) mapping results to 200, 400 and 503 problem details, and 404, and register it in `AddMarketDataModule`; verify with integration tests for each status code, for HTTP 401 without a token, for a lower-case symbol returning the upper-case one, and that two different users get identical data
- [x] 4.2 Rebuild the backend so `backend/openapi.json` includes the endpoint, regenerate the frontend client with `npm run generate:api`, and verify `npm run check:api` passes

## 5. Frontend stock lookup

- [x] 5.1 Add `frontend/src/market-data/StockLookup.tsx` (symbol form, query on submit, formatted metrics with units, "Not available" for nulls, source and as-of time, stale note, messages for invalid/unknown symbols and other errors) and render it in `App` for signed-in users; verify with Vitest + React Testing Library tests for a successful lookup, a missing metric, a stale response, a 404, a 400, and that nothing is requested before submit
- [x] 5.2 Document the market data settings (`MarketData:Provider`, `MarketData:CacheMaxAge`) and the fake provider's sample symbols in `README.md`, and verify the documented symbols return data in the running app

## 6. Integration checks

- [x] 6.1 Run the full backend suite (`dotnet test` in `backend`, `dotnet format --verify-no-changes`) and frontend checks (`npm run lint`, `npm run typecheck`, `npm test`, `npm run build:local`, `npm run check:api`), and verify all pass
- [x] 6.2 Verify end to end against the local environment: sign in as `alice`, look up `lynch`, `SPARSE`, `EUROCO` and an unknown symbol, and confirm the results match the spec scenarios; then sign in as `bob` and confirm the same data is shown

## Workflow follow-up

- Archive the change once implementation is reviewed, which creates `openspec/specs/market-data/spec.md`.
- A later change adds the first real market data vendor as another `IMarketDataProvider` adapter.
- `strategy-assessment` consumes `IMarketDataModule.GetStockFundamentalsAsync` and the fake sample companies.
