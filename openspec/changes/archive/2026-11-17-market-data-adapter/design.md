# Design

## Context

The bootstrap change left the MarketData module as an empty shell: `MarketData.Contracts` with an empty `IMarketDataModule`, an internal `MarketDataModuleFacade`, and `MarketDataDbContext` on the `market_data` schema with no entities. The conventions from bootstrap apply: modules expose endpoints through `IEndpointModule`, other modules see only `*.Contracts`, money uses `Money`/`MoneyMapping`, raw SQL and `IgnoreQueryFilters()` are allowed only in SharedKernel and Identity (architecture test), and the frontend uses the client generated from `backend/openapi.json`. See proposal.md for motivation and `specs/market-data/spec.md` for the required behaviour.

## Goals / Non-Goals

**Goals:**
- A provider interface narrow enough that adding a real vendor later means adding one adapter class and its settings, with no change to Contracts, the endpoint, the cache or the frontend.
- One lookup path, shared by the HTTP endpoint and the in-process `IMarketDataModule`, so the Assessment module gets the same caching and stale fallback as the web app.

**Non-Goals:**
- Rate limiting, retries or request batching against the provider. The fake provider needs none; the first real adapter will add what its vendor needs.
- Caching "symbol not found" answers.
- Background refresh of cached data. Data is refreshed only when it is requested after it has expired.

## Decisions

### Contracts: one DTO and one result type

`MarketData.Contracts` gets:
- `StockFundamentals`: `Symbol`, `CompanyName`, `Currency`, `Price` (`Money?`), `MarketCap` (`Money?`), `TrailingPe`, `ForwardPe`, `EpsGrowth`, `InstitutionalOwnership`, `InsiderOwnership`, `DebtToEquity` (all `decimal?`), `Source` (display name of the provider), `AsOf` (`DateTimeOffset`) and `IsStale` (`bool`).
- `StockLookupResult`: a closed set of outcomes (`Found(StockFundamentals)`, `InvalidSymbol`, `NotFound`, `Unavailable`), returned by `IMarketDataModule.GetStockFundamentalsAsync(string symbol, CancellationToken)`.

Units follow the spec: percentages as fractions, ratios as plain decimals, money in the listing currency. EPS growth is defined as the year-over-year growth of trailing-twelve-month EPS; a real adapter must convert its vendor's figure to that definition or report null. Alternative considered: throwing exceptions for not-found and unavailable. Rejected because these are normal outcomes the Assessment module must handle, and a result type makes the caller handle each one.

### Ticker symbols as a value type

An internal `TickerSymbol` value type parses input (trim, upper-case, `^[A-Z0-9][A-Z0-9.-]{0,14}$`). Every path into the module parses the symbol first, so invalid input is rejected before the cache or provider is touched (spec: Invalid symbol). Exchange suffixes such as `.DE` fit the pattern, so a later adapter for non-US markets needs no format change.

### Provider adapter interface

```csharp
internal interface IMarketDataProvider
{
    string Name { get; }          // settings key, e.g. "Fake"
    string DisplayName { get; }   // reported as Source, e.g. "Sample data (fake provider)"
    Task<ProviderFundamentals?> FetchAsync(TickerSymbol symbol, CancellationToken ct); // null = unknown symbol
}
```

A provider throws `MarketDataProviderException` (or lets `HttpRequestException`/timeouts escape, which the lookup service treats the same way) when it cannot answer. `ProviderFundamentals` is the same set of fields as `StockFundamentals` without `IsStale`.

Every provider is registered in DI as `IMarketDataProvider`. `MarketDataOptions.Provider` (setting `MarketData:Provider`) picks one by `Name`; options validation runs at startup and lists the valid names when the setting does not match one (spec: Unknown provider configured). `appsettings.json` sets `"Fake"`, since it is the only provider. Integration tests register an extra test provider and select it through settings, the same way they add test endpoints. Alternative considered: keyed services. Rejected because validation needs the list of all registered names, which is simpler with a plain enumerable.

### Fake provider

`FakeMarketDataProvider` holds a static in-code table of fictional companies, with `AsOf` set to the current time from `TimeProvider`:

| Symbol | Purpose |
|---|---|
| `LYNCH` | Meets every criterion in `docs/peter-lynch-stock-criteria.jpeg` |
| `BIGCO` | Large cap, high P/E, high institutional ownership, high debt: fails several |
| `SPARSE` | Forward P/E, institutional ownership and debt-to-equity missing |
| `LOSSCO` | Negative earnings: both P/E values null |
| `EUROCO` | Listed in EUR |

Values are chosen near the thresholds where useful, so `strategy-assessment` can use the same data for its tests. Every other symbol returns null (not found). Alternative considered: generating pseudo-random data from a hash of any symbol. Rejected because any ticker would look like a real company with made-up numbers.

### Cache in the `market_data` schema

An internal `CachedStockFundamentals` entity, mapped to `market_data.stock_fundamentals`, with the symbol as primary key, one column per metric, `Price` and `MarketCap` through `MoneyMapping`, plus `source`, `as_of` and `fetched_at`. It is not `IUserOwned`, so no owner filter applies (spec: Same data for every user).

`StockLookupService` (internal, scoped) implements the lookup:
1. Parse the symbol; invalid → `InvalidSymbol`.
2. Read the cache row. If `fetched_at` is within `MarketData:CacheMaxAge` (default 24 hours, read through `TimeProvider`) and its `source` is the configured provider, return it. Rows from a previously configured provider count as expired, so switching provider takes effect immediately; they are still used as the stale fallback in step 4.
3. Otherwise call the provider. Null → `NotFound`. Data → upsert the row and return it.
4. If the provider throws: return the cached row with `IsStale = true` if there is one, else `Unavailable`. The failure is logged with the provider name and symbol.

The upsert uses EF Core (add or update the tracked row) instead of `INSERT ... ON CONFLICT`, because raw SQL is restricted to SharedKernel and Identity. If two requests insert the same new symbol at once, the loser's unique violation is caught and its fetched data is returned anyway; the row ends up holding one of two equally fresh copies. Alternative considered: an in-memory cache. Rejected because it is lost on restart, is not shared between instances, and would spend a rate-limited vendor's daily quota again after every deploy.

### HTTP endpoint

`MarketDataEndpoints : IEndpointModule` maps `GET /market-data/stocks/{symbol}` (so `/api/market-data/stocks/{symbol}`), operation name `GetStockFundamentals`, tag `MarketData`, and maps the result: `Found` → 200, `InvalidSymbol` → 400 problem details, `NotFound` → 404, `Unavailable` → 503 problem details. Authentication comes from the host's fallback policy. The endpoint calls `IMarketDataModule`, so HTTP and in-process callers share one code path.

These are the API's first decimal values. ASP.NET Core documents decimals as "number or string" (because it also accepts strings on input), which would type every metric as `number | string` in the generated client. A schema transformer in the host's OpenAPI setup documents decimals as numbers only, which is what the API sends. Timestamps are truncated to microseconds before they are cached, so the response that fetched the data is identical to later responses read from Postgres.

### Frontend stock lookup

A `StockLookup` component in `frontend/src/market-data/` holds a symbol form and calls the generated TanStack Query hook for `GetStockFundamentals` only after submit (`enabled` on the submitted symbol). It shows a definition list of the metrics: money with `Intl.NumberFormat` in the stock's currency (market cap in compact notation), fractions as percentages, ratios to two decimals, and "Not available" for nulls; then the source and as-of time, and a stale note when `isStale` is set. A 400 or 404 response shows an inline message; other errors show a generic failure message. The same symbol pattern is checked in the browser to give feedback before sending, but the server stays authoritative. `App` renders it below the profile card for signed-in users; there is no router yet, so no new route is added.

## Risks / Trade-offs

- [Fictional fake tickers may match real tickers on some exchange] → The source is shown as "Sample data (fake provider)" on every result, and the fake provider is the only one until a real one is added.
- [Fundamentals and price share one cache age, but prices change every second during trading] → Acceptable while data is fake; the first real adapter decides whether price needs its own shorter age.
- [Unknown symbols are not cached, so repeated lookups of a bad symbol call the provider every time] → Free for the fake provider; revisit with the first rate-limited vendor.
- [Vendors define metrics differently (EPS growth period, debt measure)] → Definitions are fixed in Contracts documentation; an adapter that cannot match one returns null rather than a different measure.
- [`numeric(19,4)` limits money to below 10^15] → Larger than any market capitalisation in any currency the app expects; a stock above it would fail to cache and be returned uncached.

## Migration Plan

One new EF Core migration adds `market_data.stock_fundamentals`. It applies at startup in Development and with `dotnet ef database update` elsewhere, as set up in bootstrap. Rollback is the migration's `Down`, which drops the table; the table holds only cached data, so nothing is lost.
