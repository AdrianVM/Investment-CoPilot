# Tasks

## 1. Exchange rates (MarketData module)

- [x] 1.1 Add `ConvertAsync` and `CurrencyConversionResult` (`Converted`, `RateUnavailable`) to `MarketData.Contracts`, plus the internal `IExchangeRateProvider`, `ExchangeRate` and `FakeExchangeRateProvider` (USD, EUR, GBP, CHF, JPY, CAD; rate = `usdPer[from] / usdPer[to]` rounded to six places); verify with unit tests that rates are repeatable, reverse rates equal 1/r to six places, cross rates go through USD, and other currencies return null
- [x] 1.2 Add `MarketData:ExchangeRateProvider` (default `Fake` in `appsettings.json`) with startup validation in `MarketDataOptionsValidator`, and implement `ConvertAsync` in the facade (same currency gives rate 1 without calling the provider; a null or a thrown error gives `RateUnavailable`, and errors are logged); verify with integration tests that an unknown provider name fails startup naming the setting and valid providers, and that EUR to USD, USD to USD and an unsupported currency return the expected results

## 2. Strategy model and Peter Lynch criteria (Assessment module)

- [x] 2.1 Add the contracts (`IAssessmentModule.AssessAsync`, `AssessmentLookupResult`, `StockAssessment`, `CriterionAssessment`, `Measure`, `CurrencyConversion`, `AssessmentSummary`, string-serialized enums), and give the Assessment project references to `MarketData.Contracts` and `Microsoft.AspNetCore.App`; verify `dotnet build backend` succeeds and the architecture tests pass
- [x] 2.2 Implement `IStrategy`, `ICriterion`, `ThresholdCriterion`, `PeCriterion`, `MarketCapCriterion`, `PeterLynchStrategy` and `StrategyRegistry`, and verify with unit tests: each threshold is strict (including values exactly at the threshold); a missing value gives `CannotAssess` with `DataNotAvailable`; the three P/E cases from the spec; market cap in USD, converted from EUR (with the conversion attached), and with the rate unavailable (`ExchangeRateNotAvailable`)
- [x] 2.3 Implement `AssessmentService` (fetch fundamentals, map lookup outcomes, evaluate criteria in order, build the summary, pass through source, as-of time and stale flag) and wire it into the facade; verify with unit tests using a fake `IMarketDataModule` that the sample companies give LYNCH 6/6, BIGCO 0 met, SPARSE 2 met, 1 not met and 3 cannot assess, LOSSCO 4 met and 2 cannot assess, and EUROCO 6/6 via conversion; and that stale fundamentals give a stale assessment

## 3. HTTP endpoint

- [x] 3.1 Add `AssessmentEndpoints` (`GET /api/assessments/{strategy}/stocks/{symbol}`, operation `GetStockAssessment`) and register it; verify with integration tests: 200 for `LYNCH` with six criteria in spec order and string enum values in the JSON; 404 problems "Unknown strategy" and "Stock not found"; 400 for an invalid symbol; 503 with a failing fundamentals provider and nothing cached; and 401 without a token
- [x] 3.2 Rebuild so `backend/openapi.json` includes the endpoint, regenerate the frontend client, and verify `npm run check:api` passes (against a temporary git index if the files are not yet committed)

## 4. Frontend checklist

- [x] 4.1 Add `--color-pass`, `--color-pass-surface`, `--color-fail` and `--color-fail-surface` to `tokens.css` and their pairs to `tokens.test.ts`, and verify the contrast test passes in both themes
- [x] 4.2 Add `frontend/src/assessment/LynchChecklist.tsx` (its own query, summary sentence, one list item per criterion with verdict icon and text, values and thresholds in the value's unit, the reason when not assessable, the converted market cap with rate and source, an always-visible disclaimer, and an inline notice when the assessment fails), render it under the figure sheet in `StockLookup`, and verify with component tests for a full pass, missing data, a EUR conversion, an assessment failure with the figures still visible, and that verdicts are announced as text
- [x] 4.3 Document the strategy, its thresholds and the exchange rate setting in `README.md` and `backend/README.md`, and verify the documented sample results match what the app shows

## 5. Verification

- [x] 5.1 Run the full backend suite (`dotnet test`, `dotnet format --verify-no-changes`) and the frontend checks (`lint`, `typecheck`, `test`, `build:local`), and verify all pass
- [x] 5.2 Verify end to end against the local environment in a headless browser, in both themes: sign in, look up `LYNCH`, `SPARSE`, `EUROCO` and `LOSSCO`, check that the checklist matches the expected verdicts and summary, check that 320 px has no horizontal scrolling, and check there are no CSP errors
