# Tasks

## 1. Repository and tooling setup

- [x] 1.1 Initialise a local git repository with a `.gitignore` for .NET, Node and IDE files, and verify `git status` lists only source files
- [x] 1.2 Verify the .NET 10 SDK, Node.js 22.12+ and Docker are available (`dotnet --version`, `node --version`, `docker compose version`), and record the required versions in `README.md`
- [x] 1.3 Create `backend/InvestmentCoPilot.sln` with the Host, SharedKernel, `Identity`/`Identity.Contracts`, `MarketData`/`MarketData.Contracts` and `Assessment`/`Assessment.Contracts` projects and the three test projects from the design layout, and verify `dotnet build backend` succeeds
- [x] 1.4 Scaffold `frontend/` with Vite (React + TypeScript), oxlint (ESLint's TypeScript plugin does not support TypeScript 7 yet), Vitest and React Testing Library, and verify `npm run build`, `npm run lint` and `npm test` succeed. (Since task 8.4, `npm run build` intentionally requires production sign-in settings; `npm run build:local` is the local equivalent. See design: Content Security Policy.)

## 2. Local environment

- [x] 2.1 Add `docker-compose.yml` with Postgres 17 and Keycloak (dev mode), and a realm export with the API audience, the SPA client (Authorization Code + PKCE) and two test users; verify `docker compose up` starts both and the realm's discovery document loads
- [x] 2.2 Document starting the local environment and the test user credentials in `README.md`, and verify the documented commands work from a clean checkout

## 3. Shared kernel and module boundaries

- [x] 3.1 Implement `Money` (decimal amount + ISO 4217 currency, mapped to `numeric(19,4)` plus currency) with unit tests for equality, rounding and mismatched-currency arithmetic, and verify they pass
- [x] 3.2 Implement `ICurrentUser`, `IUserOwned` and the module base DbContext (per-module default schema and migrations history table), and verify with a unit test that each module context uses its own schema
- [x] 3.3 Give each module its DbContext (`identity`, `market_data`, `assessment`), an initial migration and an empty public interface in its Contracts project, and verify `dotnet ef migrations list` works for each context
- [x] 3.4 Add architecture tests (NetArchTest): no module implementation references another module's implementation, no module uses another module's DbContext, and `IgnoreQueryFilters`/raw SQL appear only in SharedKernel and Identity; verify they pass, and fail when a forbidden reference is added temporarily
- [x] 3.5 Apply migrations at host startup in Development only, and verify with a Testcontainers integration test that all three schemas exist after startup

## 4. API authentication

- [x] 4.1 Configure JWT bearer validation from `Auth:Authority` and `Auth:Audience` settings, with a fallback policy requiring authentication, and verify there is no provider-specific code or package
- [x] 4.2 Add anonymous `/health/live` and `/health/ready` endpoints (ready checks Postgres), and verify they respond without a token
- [x] 4.3 Build the integration test harness (`WebApplicationFactory` + Testcontainers Postgres + a test issuer that mints JWTs with a test signing key), and verify a test can call the API as a chosen user
- [x] 4.4 Write integration tests for the "API requires authentication" and "Access tokens are validated against the configured provider" scenarios (no token, valid, expired, wrong issuer, wrong audience, tampered, health check), and verify they pass
- [x] 4.5 Verify the "Switching providers" scenario: run the API against the test issuer and against local Keycloak with only configuration changed, with no code changes in between

## 5. Local user provisioning and profile

- [x] 5.1 Implement the Identity `User` entity (application-generated UUID, issuer, subject, email, display name) with a unique index on `(issuer, subject)` and its migration, and verify the migration applies
- [x] 5.2 Implement the provisioning middleware (insert with `ON CONFLICT DO NOTHING` then re-read; update email and display name from configurable claim names), exposing the local user ID through `ICurrentUser`, and verify with integration tests for first request, returning user, changed claims and missing optional claims
- [x] 5.3 Add an integration test that sends concurrent first requests for one new identity, and verify exactly one user exists and every request succeeds
- [x] 5.4 Add `GET /api/me` returning user ID, email and display name, and verify with integration tests for the "Current user can view their profile" scenarios

## 6. User data scoping

- [x] 6.1 Add the global `OwnerId` query filter for `IUserOwned` entities, plus `SaveChanges` logic that sets `OwnerId` on insert and rejects changes to it, and verify with unit tests on an in-test entity
- [x] 6.2 Add a test-only user-owned entity and endpoints in the integration test project, and verify every "User-owned data is accessible only to its owner" scenario (own list, read/update/delete another user's record returns 404 and leaves it unchanged, client-supplied owner ignored)
- [x] 6.3 Document the user-owned data convention (implement `IUserOwned`, never bypass the filter, cross-user access returns 404) in `backend/README.md`, and verify it matches the tested behaviour

## 7. OpenAPI and generated client

- [x] 7.1 Publish the OpenAPI document with `Microsoft.AspNetCore.OpenApi`, including the bearer security scheme, and write it to `backend/openapi.json` on build; verify the file contains `/api/me`
- [x] 7.2 Configure `@hey-api/openapi-ts` to generate the typed client and TanStack Query hooks into `frontend/src/api/generated/` with an `npm run generate:api` script, commit the output, and verify the frontend type-checks against it
- [x] 7.3 Add a `check:api` script that regenerates the document and client and fails on any diff, and verify it fails after a deliberate API change and passes after regeneration

## 8. Frontend sign-in

- [x] 8.1 Configure `oidc-client-ts` (Authorization Code + PKCE, in-memory storage, silent renew) from Vite environment variables, with callback and silent-renew routes, and verify there is no provider-specific code
- [x] 8.2 Set up TanStack Query with the generated client sending the access token, and show the signed-in user's profile from `/api/me`
- [x] 8.3 Add sign-in and sign-out, with component tests for the signed-out, signed-in and signed-out-again states, and verify they pass
- [x] 8.4 Add a strict Content Security Policy for the built app, and verify the app still signs in under it
- [x] 8.5 Verify end to end against local Keycloak: each test user signs in, sees only their own profile, and signs out

## 9. Continuous integration

- [x] 9.1 Add `.github/workflows/ci.yml` running backend build, `dotnet format --verify-no-changes`, unit, integration and architecture tests; frontend lint, type check, tests and build; and `check:api`; verify every step passes locally with the same commands
- [x] 9.2 Verify the full suite from a clean clone: `docker compose up`, backend and frontend tests, and a manual sign-in all work by following only `README.md`

## Workflow follow-up

- Create a GitHub remote and push, so the CI workflow actually runs (left to the user).
- Choose the production identity provider (design Open Question); only configuration changes.
- Archive the change once implementation is reviewed, which creates `openspec/specs/user-identity/spec.md`.
