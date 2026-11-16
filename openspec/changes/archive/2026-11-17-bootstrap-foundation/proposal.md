# Proposal

## Why

Investment Co-Pilot will grow from a personal stock assessment tool into a multi-user application. The decisions that are hardest to change later (module boundaries, per-user data ownership, authentication, a typed contract between the frontend and backend) must be in place before any feature work starts, so that market data and strategy assessment, and later features such as portfolio tracking, can be built on a stable base.

## What Changes

- Create the backend as an ASP.NET Core Web API on .NET 10 LTS, structured as a modular monolith: one deployable host plus one project per module (Identity, MarketData, Assessment). Modules interact only through public interfaces, never through each other's tables.
- Create the frontend as a React + TypeScript single-page app (Vite), using TanStack Query for server state.
- Use PostgreSQL through EF Core, with versioned migrations. Each module owns its own schema.
- Authenticate through an external OAuth 2.0 / OpenID Connect provider, kept provider-agnostic: the backend validates standard JWT access tokens and the frontend uses the standard Authorization Code + PKCE flow, both configured only through settings (issuer, audience, client ID). No specific provider is chosen yet, and switching providers must not require code changes. The API accepts only authenticated requests, except for health checks. A local user record is created on a user's first sign-in.
- Require every user-owned record to carry an owning user ID, and have queries scope data to the current user by default.
- Publish an OpenAPI document from the backend and generate the TypeScript API client from it, so frontend and backend types cannot drift apart.
- Provide local development via Docker Compose (Postgres and a development-only OIDC provider).
- Set up testing: xUnit unit tests and Testcontainers-based integration tests against real Postgres, plus frontend tests.
- Set up CI to build, lint, test, and check that the generated API client is up to date.
- Add architecture tests that fail the build when one module references another module's internals.

Out of scope: market data and assessment features (planned as later changes `market-data-adapter`, `strategy-assessment`); portfolio tracking (deferred, with no module created now; the module layout makes it straightforward to add later); billing, deployment to production hosting, and mobile apps.

## Capabilities

### New Capabilities
- `user-identity`: Authentication of users through an external OIDC provider, creation of a local user on first sign-in, the current-user profile, and the rule that user-owned data is accessible only to its owner.

### Modified Capabilities
- None (no existing specs).

## Impact

- New repository layout: `backend/` (.NET solution), `frontend/` (Vite app), `docker-compose.yml`, CI workflow.
- New dependencies: .NET 10 SDK, ASP.NET Core, EF Core + Npgsql, xUnit, Testcontainers, an OpenAPI generator, React, TypeScript, Vite, TanStack Query, and a standards-based OIDC client library (no provider-specific SDK).
- Establishes conventions every later change must follow: module boundaries, user-scoped data, decimal money values, and the generated API client.
