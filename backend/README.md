# Backend

ASP.NET Core Web API on .NET 10, built as a modular monolith: one deployable host plus one project pair per module.

```
src/
  Host/                      composition root: authentication, health checks, endpoint mapping
  SharedKernel/              cross-module primitives: ICurrentUser, IUserOwned, Money, ModuleDbContext
  Modules/<Module>/
    <Module>.Contracts/      the module's public interface; the only project other modules may reference
    <Module>/                the implementation; its types are internal
tests/
  UnitTests/  IntegrationTests/  ArchitectureTests/
```

## Commands

Run from `backend/`. The integration tests need Docker running; they start their own Postgres.

```bash
dotnet build
dotnet test
dotnet run --project src/Host          # needs `docker compose up -d --wait` from the repository root
```

Migrations are per module. For example, to add one to the Identity module:

```bash
dotnet ef migrations add <Name> --project src/Modules/Identity/Identity --startup-project src/Host \
  --context IdentityDbContext --output-dir Persistence/Migrations
```

`dotnet ef` is a local tool; run `dotnet tool restore` from the repository root once. In Development the API applies pending migrations at startup. Other environments must run `dotnet ef database update` (per context) as an explicit step.

## Module rules

These rules are enforced by `tests/ArchitectureTests`, so breaking one fails the build:

- A module references other modules only through their `*.Contracts` projects.
- A module never uses another module's DbContext or tables. Each module has its own Postgres schema.
- `SharedKernel` depends on no module.

## User-owned data

Every record that belongs to a user must be visible to, and changeable by, that user only. The module DbContext enforces this, so endpoints don't need their own ownership checks:

1. **Implement `IUserOwned`** on every entity that belongs to a user, and put it in a DbContext derived from `ModuleDbContext<T>`. Configure the model in `ConfigureModel`, not `OnModelCreating`.
2. **Queries are filtered automatically.** Every query on the entity is restricted to the current user by a named query filter (`UserOwned`), including `Find`, `ExecuteUpdate` and `ExecuteDelete`. With no signed-in user, queries return nothing.
3. **The owner is set on insert.** `OwnerId` is always set from the current user when the record is saved, whatever the request contained. Changing `OwnerId` afterwards throws.
4. **Another user's record returns 404**, exactly as if it didn't exist. Look the record up through the filtered context and return 404 when it isn't found. Never return 403 for it, because that would reveal that the record exists.
5. **Never bypass the filter.** `IgnoreQueryFilters()` and raw SQL (`FromSql*`, `SqlQuery*`, `ExecuteSql*`) skip it, so they are allowed only in SharedKernel and Identity, which the architecture tests check.
6. **Test cross-user access** for every endpoint that touches user-owned data: another user's record must return 404 and be left unchanged.

These rules are covered by `tests/UnitTests/SharedKernel/UserOwnershipTests.cs` and `tests/IntegrationTests/Scoping/UserOwnedDataTests.cs`.

## Money

Use `Money` (a `decimal` amount plus an ISO 4217 currency code) for every monetary value, never `double` or `float`. Map it with `builder.ComplexProperty(x => x.Price, MoneyMapping.Configure)`, which stores `numeric(19,4)` plus a `char(3)` currency. Amounts are rounded to 4 decimal places with banker's rounding, and combining two currencies throws.

## Authentication

The API validates JWT access tokens from any standards-compliant OpenID Connect provider, configured only through settings:

| Setting | Meaning |
|---|---|
| `Auth:Authority` | The provider's issuer URL |
| `Auth:Audience` | The audience tokens must be issued for |
| `Auth:RequireHttpsMetadata` | `true` except for a local development provider |
| `Auth:MetadataAddress` | Optional. Where to download the provider's discovery document and signing keys, when the API reaches the provider at another address than the issuer in tokens (for example `http://keycloak:8080/...` inside Docker Compose). Tokens are still validated against the issuer. Unset: derived from `Auth:Authority`. |
| `Auth:Claims:Email`, `Auth:Claims:DisplayName` | Claim names for the user's email and name (defaults `email`, `name`) |

Every endpoint requires a valid token, except `/health/live` and `/health/ready`. On a user's first request, the Identity module creates a local user, keyed by the token's issuer and subject. Code reads the current user through `ICurrentUser`.

Modules add HTTP endpoints by registering an `IEndpointModule`; the host maps them all under `/api`.

## Market data

The MarketData module looks up a stock's current fundamentals by ticker symbol, through `IMarketDataModule` in-process and `GET /api/market-data/stocks/{symbol}` over HTTP.

| Setting | Meaning |
|---|---|
| `MarketData:Provider` | Which data source to use. Startup fails if it names an unknown one. Only `Fake` exists so far: fixed sample data for `LYNCH`, `BIGCO`, `SPARSE`, `LOSSCO` and `EUROCO`. |
| `MarketData:CacheMaxAge` | How long fetched data is reused, shared by all users (default `1.00:00:00`, one day). After that it is fetched again; if the provider then fails, the old data is returned marked stale. |
| `MarketData:ExchangeRateProvider` | Which exchange rate source to use. Startup fails if it names an unknown one. Only `Fake` exists so far: fixed sample rates between USD, EUR, GBP, CHF, JPY and CAD. Other currencies have no rate. |

Units are fixed for every provider: money in the stock's listing currency, ratios as plain numbers, percentages as fractions (0.15 = 15 %), and anything a provider cannot supply as `null`, never zero.

To add a real data source, implement the internal `IMarketDataProvider` (in `Providers/`), register it in `MarketDataModule`, and select it with `MarketData:Provider`. Nothing else changes.

`IMarketDataModule.ConvertAsync` converts an amount to another currency and reports the rate, its source and as-of time. When no rate is available it says so; it never guesses or falls back to a rate of 1.

## Strategy assessment

The Assessment module checks a stock against an investment strategy, on demand, through `IAssessmentModule.AssessAsync` and `GET /api/assessments/{strategy}/stocks/{symbol}`. Nothing is stored.

The only strategy is `peter-lynch`, with these strict thresholds (a value equal to the threshold does not meet it):

| Criterion | Meets when |
|---|---|
| P/E | trailing P/E < 25 **and** forward P/E < 15 |
| Institutional ownership | < 0.10 (10 %) |
| Insider ownership | > 0.20 (20 %) |
| EPS growth | > 0.15 (15 %) |
| Debt to equity | < 0.35 |
| Market capitalisation | < 5,000,000,000 USD, converting other currencies at the current rate |

A missing value makes a criterion "cannot be assessed", never "does not meet". The P/E criterion fails if either P/E present is too high, and is met only when both are present and low enough. A market cap that cannot be converted (no rate) cannot be assessed.

To add a strategy, implement `IStrategy` (in `Strategies/`) as an ordered list of `ICriterion`, and register it in `AssessmentModule`. The endpoint, contracts and web app need no changes.
