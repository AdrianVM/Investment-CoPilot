# Design

## Context

The Assessment module is a stub: an empty `IAssessmentModule`, an unused `AssessmentDbContext` and no endpoints. The MarketData module exposes `IMarketDataModule.GetStockFundamentalsAsync`, which returns a `StockLookupResult` (`Found`, `InvalidSymbol`, `NotFound`, `Unavailable`) with fundamentals in fixed units (percentages as fractions, money in the listing currency, missing values as null). Modules may reference only each other's `*.Contracts` projects (architecture tests). The frontend shows a looked-up stock in `StockLookup` with the `FigureSheet`, `Notice` and token system from `visual-design`. See proposal.md for motivation and the two delta specs for the required behaviour.

## Goals / Non-Goals

**Goals:**
- Adding a second strategy later means adding one strategy definition (a list of criteria), with no change to the endpoint, contracts or frontend component.
- Exchange rates follow the same provider pattern as fundamentals, so a real rate vendor is one more adapter.

**Non-Goals:**
- Caching exchange rates. The fake provider is in memory; the first real rate vendor adds caching like the fundamentals cache.
- Historical rates, or converting price or anything other than the market cap criterion.
- Persisting assessments; the `assessment` schema stays empty.

## Decisions

### Exchange rates in the MarketData module

Contracts (`MarketData.Contracts`):

```csharp
Task<CurrencyConversionResult> ConvertAsync(Money amount, string toCurrency, CancellationToken ct); // on IMarketDataModule

abstract record CurrencyConversionResult
{
    sealed record Converted(Money Amount, decimal Rate, string Source, DateTimeOffset AsOf);
    sealed record RateUnavailable(string FromCurrency, string ToCurrency);
}
```

Internally, `IExchangeRateProvider { Name; DisplayName; Task<ExchangeRate?> GetRateAsync(from, to, ct) }`, where null means no rate. The provider is selected by `MarketData:ExchangeRateProvider` (default `Fake` in `appsettings.json`) and validated at startup by the existing `MarketDataOptionsValidator`, extended to list the valid rate providers. Same-currency conversion returns rate 1 without calling the provider. A provider that throws is treated as `RateUnavailable` and logged, so a rate outage makes the market cap criterion unassessable instead of failing the whole assessment.

`FakeExchangeRateProvider` holds US dollars per unit for USD 1, EUR 1.0850, GBP 1.2700, CHF 1.1300, JPY 0.006700 and CAD 0.7300. Any pair's rate is `usdPer[from] / usdPer[to]` rounded to six decimal places, so reverse and cross rates are consistent (spec: Reverse direction). `AsOf` is the current time, and the source is "Sample rates (fake provider)". Alternative considered: a separate FX module. Rejected because rates are market data, and a new module would need its own Contracts, DbContext and settings for one method.

### Strategy model in the Assessment module

```
Assessment/
  Strategies/
    IStrategy.cs            # Id, Name, Criteria
    ICriterion.cs           # Id, Name, EvaluateAsync(stock, marketData, ct)
    ThresholdCriterion.cs   # one nullable measure vs one threshold
    PeCriterion.cs          # trailing + forward P/E rule
    MarketCapCriterion.cs   # converts to USD, then compares
    PeterLynchStrategy.cs   # id "peter-lynch", the six criteria in order
    StrategyRegistry.cs     # id → strategy
  AssessmentService.cs      # fetch fundamentals, run criteria, summarise
  AssessmentEndpoints.cs
```

Criteria are small classes evaluated in order. Evaluation is async because the market cap criterion calls `IMarketDataModule.ConvertAsync`. Thresholds are `const decimal` values in `PeterLynchStrategy`, in the same units as the fundamentals (fractions, not percentages). Comparisons are strict (spec: Value exactly at the threshold). Alternative considered: a declarative rules table (JSON) evaluated generically. Rejected because the P/E and market cap rules are not single comparisons, and a rules language is premature for one strategy.

Verdict rules (spec: P/E criterion, Missing data):
- `ThresholdCriterion`: a null value gives `CannotAssess` (`DataNotAvailable`); otherwise `Meets` or `DoesNotMeet`.
- `PeCriterion`: any present value at or above its threshold gives `DoesNotMeet`; both present and below gives `Meets`; otherwise `CannotAssess` (`DataNotAvailable`).
- `MarketCapCriterion`: a null market cap gives `DataNotAvailable`. A USD market cap is compared directly. Otherwise it converts to USD; `RateUnavailable` gives `CannotAssess` (`ExchangeRateNotAvailable`), and `Converted` compares the USD amount and attaches the conversion.

### Assessment contracts and endpoint

`Assessment.Contracts`:

```csharp
Task<AssessmentLookupResult> AssessAsync(string strategyId, string symbol, CancellationToken ct); // IAssessmentModule

record StockAssessment(string StrategyId, string StrategyName, string Symbol, string CompanyName,
    string Source, DateTimeOffset AsOf, bool IsStale,
    IReadOnlyList<CriterionAssessment> Criteria, AssessmentSummary Summary);
record CriterionAssessment(string Id, string Name, Verdict Verdict, IReadOnlyList<Measure> Measures,
    UnassessableReason? Reason, CurrencyConversion? Conversion);
record Measure(string Id, string Label, decimal? Value, MeasureUnit Unit, string? Currency,
    Comparison Comparison, decimal Threshold);
record CurrencyConversion(Money Original, Money Converted, decimal Rate, string Source, DateTimeOffset AsOf);
record AssessmentSummary(int Met, int NotMet, int CannotAssess, int Total);
enum Verdict { Meets, DoesNotMeet, CannotAssess }
enum UnassessableReason { DataNotAvailable, ExchangeRateNotAvailable }
enum MeasureUnit { Ratio, Fraction, Money }
enum Comparison { LessThan, GreaterThan }
```

Enums carry `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` and a `[JsonStringEnumMemberName]` per value, so the JSON and the generated TypeScript client use camel-case string values (`"meets"`, `"lessThan"`, …) without changing global serializer settings. `AssessmentLookupResult` mirrors `StockLookupResult` (with `StockNotFound` for clarity) plus an `UnknownStrategy` case. These are the API's first integers outside identity data, so the host's OpenAPI schema transformer, which already documents decimals as numbers only, now covers every numeric type; otherwise the summary counts would be typed `number | string` in the generated client. The fundamentals' `IsStale`, `AsOf` and `Source` pass through unchanged (spec: Assessments reflect the age of the data). The module does not store anything.

`AssessmentEndpoints` maps `GET /assessments/{strategy}/stocks/{symbol}` (operation `GetStockAssessment`, tag `Assessment`): `Found` gives 200; `UnknownStrategy` gives a 404 problem titled "Unknown strategy"; `NotFound` gives a 404 problem titled "Stock not found"; `InvalidSymbol` gives 400; `Unavailable` gives 503. The 404s carry problem details so a client can tell them apart. The Assessment project gains a reference to `MarketData.Contracts` and `Microsoft.AspNetCore.App`.

### Frontend: the checklist under the figure sheet

`frontend/src/assessment/LynchChecklist.tsx` takes a symbol and queries `GetStockAssessment` for `peter-lynch` in parallel with the fundamentals (separate TanStack Query, so one failing does not hide the other; spec: Assessment unavailable). `StockLookup` renders it below the `FigureSheet` once the fundamentals are found.

```
Peter Lynch checklist                     Meets 4 of 6 criteria. 1 could not be assessed.
─────────────────────────────────────────────────────────────────────────────
✓ Meets            P/E                 Trailing 14.20 (below 25), forward 11.80 (below 15)
✗ Does not meet    Debt to equity      1.45 (below 0.35 required)
? Cannot assess    Institutional own.  Data not available
✓ Meets            Market cap          €3.2B, about $3.5B at 1 EUR = 1.085 USD (below $5B)
─────────────────────────────────────────────────────────────────────────────
For information only, not investment advice. Sample rates (fake provider), as of …
```

- Verdicts render as an inline SVG icon plus text ("Meets", "Does not meet", "Cannot be assessed"), so they never rely on colour (visual-design: Status is not shown by colour alone).
- New tokens `--color-pass` and `--color-fail` with matching `-surface` values, added to `tokens.css` and its contrast test pairs. "Cannot be assessed" uses the muted text colour. This is the use the visual design reserved green and red for.
- Values use the existing `format.ts` helpers and the `.figure` tabular class; thresholds are formatted in the same unit as the value.
- The disclaimer line is always shown with the checklist.
- A criterion that cannot be assessed still lists any values that are known ("Trailing P/E 19.70 (below 25), Forward P/E not available"); only when no value is known, or no exchange rate exists, does it show just the reason.
- Both query functions throw a shared `ApiStatusError` carrying the HTTP status, so the query client's existing rule of not retrying 4xx responses applies to both.
- The list is a `<ul>` of `<li>`, with the criterion name as the label and the verdict text read first, so a screen reader hears "Meets, P/E, trailing 14.20 …".

## Risks / Trade-offs

- [Fake rates look like real rates] → Every converted amount names its source "Sample rates (fake provider)", like the fake fundamentals.
- [Lynch's rules of thumb are simplifications; users may treat "Meets 6 of 6" as a buy signal] → The disclaimer is always shown, and verdict wording is descriptive ("meets"), never advisory ("buy").
- [Two requests per lookup (fundamentals and assessment) fetch the fundamentals twice on the server] → The second fetch is a cache hit in the shared fundamentals cache; acceptable.
- [String enums in contracts] → Serialization is set per type, so other modules' JSON is unaffected; the OpenAPI document shows them as string enums for the generated client.
- [The market cap threshold is in USD, the other thresholds are unitless] → Only the market cap criterion needs currency handling; it is isolated in `MarketCapCriterion`.

## Migration Plan

No database migration. New setting `MarketData:ExchangeRateProvider` defaults to `Fake` in `appsettings.json`; startup fails with a clear message if an unknown value is configured. Rollback is reverting the change; nothing is stored.
