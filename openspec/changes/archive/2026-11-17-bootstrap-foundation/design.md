# Design

## Context

The project was greenfield when this change started: there was no code, only this OpenSpec setup and reference material in `docs/`. See proposal.md (Why) for the motivation. The stack is fixed by the proposal: .NET 10 LTS backend, React + TypeScript frontend, PostgreSQL, and an external OpenID Connect provider that is not chosen yet. Behavioural requirements are in `specs/user-identity/spec.md`.

## Goals / Non-Goals

**Goals:**
- A repository skeleton where later changes (`market-data-adapter`, `strategy-assessment`) only add code inside their own module.
- Module boundaries and user data scoping that are enforced automatically (by tests and by the data layer), not by convention alone.
- A one-command local environment that needs no external accounts.

**Non-Goals:**
- Any MarketData or Assessment behaviour. Those modules get a project, a database schema and an empty public interface only.
- Roles, permissions or admin features. Every authenticated user has the same rights over their own data.
- Production hosting, secrets management for production, and deployment pipelines.

## Decisions

### Repository layout

```
backend/
  InvestmentCoPilot.sln
  openapi.json                    # generated on build; source for the frontend client
  src/
    Host/                         # ASP.NET Core host: composition root, auth, OpenAPI, health checks
    SharedKernel/                 # cross-module primitives: ICurrentUser, IUserOwned, Money, ModuleDbContext, IEndpointModule
    Modules/
      Identity/
        Identity.Contracts/       # public interface of the module
        Identity/                 # internal implementation
      MarketData/{MarketData.Contracts, MarketData}/
      Assessment/{Assessment.Contracts, Assessment}/
  tests/
    UnitTests/  IntegrationTests/  ArchitectureTests/
frontend/                         # Vite + React + TypeScript
  src/api/generated/              # generated client, committed
  tools/openapi-codegen/          # isolated install of the client generator (see "OpenAPI and generated client")
dev/keycloak/                     # local-only Keycloak realm export
scripts/check-api.sh              # generated-client drift check
docker-compose.yml
.github/workflows/ci.yml
```

Each module is split into a `*.Contracts` project (public interfaces and DTOs) and an implementation project whose types are `internal`. Other modules may reference only `*.Contracts`. Alternative considered: one project per module with `internal` visibility only. Rejected because a project reference still exposes public types by accident; separate Contracts projects make the allowed surface explicit and easy to test.

### Module boundaries are enforced by architecture tests

The `ArchitectureTests` project fails the build when a boundary is crossed. Each rule uses the check that fits it:
- **No module references another module's implementation:** checked on each compiled assembly's references. A NetArchTest namespace rule was not used here because the namespace `InvestmentCoPilot.MarketData` is also a prefix of the allowed `InvestmentCoPilot.MarketData.Contracts`.
- **No module uses another module's persistence (DbContext, entities):** NetArchTest, on the `<Module>.Persistence` namespaces.
- **`IgnoreQueryFilters()` and raw SQL only in SharedKernel and Identity:** NetArchTest sees type dependencies, not individual method calls, so this rule scans the compiled IL with Mono.Cecil for calls to the filter-bypassing EF Core methods.

Alternative considered: code review only. Rejected because the proposal requires the build to fail.

### Modules contribute endpoints through `IEndpointModule`

A module exposes HTTP endpoints by registering an `IEndpointModule` (SharedKernel) in DI; the host maps every registered one under `/api`, where the fallback policy requires authentication. The host therefore needs no knowledge of a module's endpoints, and integration tests add test-only endpoints the same way. Alternative considered: each module exposing a `MapXxxEndpoints()` extension the host calls. Rejected because it makes the host list every module's endpoints and gives tests no hook.

### One DbContext and one Postgres schema per module

Each module has its own `DbContext`, derived from `ModuleDbContext<T>`, with its own default schema (`identity`, `market_data`, `assessment`) and its own migrations history table in that schema. All modules share one database and the connection string `ConnectionStrings:Database`, which is read when a context is created rather than when services are registered, and validated at startup. Alternative considered: one shared DbContext. Rejected because it lets any module query any table, which breaks the module boundary. Separate databases per module were rejected as unnecessary operational cost at this size.

Migrations are generated per DbContext and applied at host startup in Development, and by an explicit `dotnet ef database update` step otherwise, so that production schema changes are never applied implicitly.

### Authentication: standard JWT bearer validation

The host uses ASP.NET Core's `JwtBearer` handler configured from settings only: `Auth:Authority` (issuer), `Auth:Audience`, and `Auth:RequireHttpsMetadata` (off only for a local provider). Signing keys come from the issuer's OIDC discovery document, so no provider SDK is needed. Inbound claim mapping is off, so the standard `sub`, `email` and `name` claims keep their names. Clock skew is zero, so a token is rejected as soon as its expiry passes (spec: Expired token). A fallback authorization policy requires an authenticated user on every endpoint; health check endpoints opt out with `AllowAnonymous`. Changing provider means changing these settings only.

The frontend uses `oidc-client-ts` (Authorization Code + PKCE, in-memory token storage, silent renew in a hidden iframe), configured from `VITE_OIDC_AUTHORITY` and `VITE_OIDC_CLIENT_ID`; redirect URIs are derived from the app's origin (`/callback`, `/silent-callback`). Sign-out discards the tokens locally first, so the app shows the signed-out state even if the provider is unreachable, then ends the provider session with the ID token as a hint, which lets the provider redirect back without a confirmation page. Cached API data is cleared on sign-out. Alternative considered: a backend-for-frontend with cookie sessions. Rejected for now because it adds a server-side session store; the JWT approach can move behind a BFF later without changing the API.

### Local user provisioning

A middleware in the Identity module runs after authentication and before authorization. It looks up the user by `(issuer, subject)` and, if not found, inserts one. The pair has a unique index; the insert uses `INSERT ... ON CONFLICT DO NOTHING` followed by a re-read, so concurrent first requests produce exactly one user (spec: Simultaneous first requests). The same step updates email and display name when the token's claims differ from the stored values; the claim names default to `email` and `name` and are configurable (`Auth:Claims:Email`, `Auth:Claims:DisplayName`). The resolved local user ID is exposed to all modules through `ICurrentUser` in SharedKernel.

Local user IDs are UUIDs generated by the application, never the provider's subject, so that switching providers does not change user IDs used as foreign keys across modules.

### User data scoping through a marker interface and global query filters

User-owned entities implement `IUserOwned { Guid OwnerId }`. `ModuleDbContext<T>`:
- adds a named query filter (`UserOwned`, EF Core 10 named filters) `OwnerId == currentUser.Id` to every `IUserOwned` entity, so reads, updates and deletes of another user's record find nothing, which surfaces as HTTP 404. With no authenticated user the filter matches nothing, so a query fails closed. Because the filter is named, a module can add its own filters (for example soft delete) without replacing it; EF Core refuses to mix named and unnamed filters on one entity, so a conflicting unnamed filter fails loudly;
- indexes `OwnerId` on every `IUserOwned` entity;
- sets `OwnerId` from `ICurrentUser` on insert, ignoring any client-supplied value, and throws if it is changed afterwards.

Derived contexts configure their entities in `ConfigureModel`; `OnModelCreating` is sealed so the ownership rules are always applied after the module's own configuration.

Alternative considered: filtering in every repository method. Rejected because one forgotten filter leaks data. Bypassing the filter needs an explicit `IgnoreQueryFilters()` or raw SQL, which the architecture tests restrict to SharedKernel and Identity.

### Money as decimal

All monetary amounts use a `Money` value type (`decimal` amount + ISO 4217 currency code), stored as `numeric(19,4)` plus a `char(3)` currency column through `MoneyMapping.Configure`. Amounts are rounded to four places with banker's rounding, and arithmetic across currencies throws. This is set now because later modules depend on it.

### OpenAPI and generated client

The host publishes its OpenAPI document (with the bearer security scheme) using the built-in `Microsoft.AspNetCore.OpenApi`; in Development it is also served at `/openapi/v1.json`. `Microsoft.Extensions.ApiDescription.Server` writes the document to `backend/openapi.json` on every build. That generator only re-runs when the Host assembly changes, so an extra build target also invalidates it when any referenced module assembly is newer; otherwise an endpoint change inside a module would leave the document stale. The generator starts the host without real configuration, so startup validation and migrations are skipped while it runs.

The frontend generates its typed client and TanStack Query hooks from that file with `@hey-api/openapi-ts`. The generator needs the TypeScript compiler's JavaScript API, which TypeScript 7 (used by the app) no longer provides, so it is installed in its own package, `frontend/tools/openapi-codegen/`, pinned to TypeScript 6; `npm ci` installs it automatically. The generated code bundles its own HTTP runtime and imports only `@tanstack/react-query` from the app. It is committed so the frontend builds without running the backend. `scripts/check-api.sh` (`npm run check:api`, also in CI) regenerates both files and fails if they differ from git or if new generated files are untracked.

### Frontend tooling

Linting uses oxlint, which the Vite template ships with. ESLint was planned, but its TypeScript plugin (typescript-eslint) does not support TypeScript 7 yet. oxlint runs with the TypeScript, React, accessibility, import and Vitest plugins, and warnings fail the lint step.

### Content Security Policy

The built app carries a strict Content Security Policy as a `<meta>` tag, injected at build time only, because the Vite dev server needs inline scripts for hot reloading. Scripts and styles may come only from the app itself. `connect-src` allows the app, the sign-in provider and (if set) the API origin; `frame-src` allows the app itself and the provider, because the silent-renew iframe opens at the provider and is redirected back to the app's `/silent-callback`. The allowed origins are derived from the same environment variables as the sign-in settings.

Because the policy is built from the provider settings, `npm run build` fails until production settings are supplied; it never falls back to the development provider. `npm run build:local` builds with the development settings for local preview, and CI uses it as a compile check.

### Local development

`docker-compose.yml` runs Postgres 17 (host port 5433, so it does not clash with a locally installed Postgres) and Keycloak in dev mode, with a realm export that pre-creates the API audience, the web client and two test users. Keycloak is used only locally; nothing in the code depends on it. Two test users exist so ownership scoping can be checked by hand. The API listens on port 5080 and the web app on 5173; the Vite server forwards `/api` to the API, so the browser makes same-origin requests and no CORS configuration is needed.

### Testing

- Unit tests (xUnit) for SharedKernel and module logic.
- Integration tests (xUnit + Testcontainers Postgres + `WebApplicationFactory`) for the user-identity scenarios. Tests mint their own JWTs with a test signing key and point the API at a test issuer, so they need no running identity provider.
- Frontend tests with Vitest and React Testing Library, using a fake sign-in manager.

### CI

GitHub Actions: backend build, `dotnet format --verify-no-changes`, unit, integration and architecture tests; frontend lint, type check, tests and `build:local`; and the generated-client drift check. Node's version comes from `.nvmrc`, and .NET's from `backend/global.json`.

## Risks / Trade-offs

- [Global query filters are bypassed by raw SQL and `IgnoreQueryFilters()`] → Architecture test restricts `IgnoreQueryFilters` and raw SQL APIs to SharedKernel and Identity; integration tests cover cross-user access for every user-owned endpoint.
- [Tokens held in browser memory are lost on reload and exposed to XSS on the page] → Silent renew re-acquires tokens after reload; a strict Content Security Policy is enforced; moving to a BFF stays possible without API changes.
- [Silent renew relies on the provider's session cookie inside an iframe; if the provider is on a different site and the browser blocks third-party cookies, users must sign in again after every reload] → Choose a production provider on the same site (for example a custom domain such as `login.<app domain>`), or move to a BFF.
- [A `<meta>` Content Security Policy cannot set `frame-ancestors`, so it cannot stop other sites from framing the app] → Send the policy, including `frame-ancestors 'none'`, as an HTTP header when production hosting is set up.
- [TypeScript 7 tooling lags behind: typescript-eslint and the client generator do not support it yet] → oxlint for linting; the generator runs in an isolated package on TypeScript 6. Revisit both when the tools support TypeScript 7.
- [Providers differ in claim names, e.g. some put no `email` in access tokens] → Claim names for email and display name are configurable, with OIDC standard names as defaults; missing optional claims leave the stored value unchanged.
- [Applying migrations at startup is unsafe with several instances] → Done only in Development; other environments apply migrations as an explicit step.
- [Committed generated code can cause merge conflicts] → It is regenerated, never hand-edited, and CI checks it is current, so conflicts are resolved by regenerating.
- [CI cannot run until the repository has a GitHub remote] → Creating the remote is left to the user; every CI step has been run locally with the same commands.

## Open Questions

- Which production identity provider to use (for example Auth0, Entra ID, Keycloak or Zitadel). This needs only configuration and does not change the specs or tasks; see the silent-renew risk above when choosing.
