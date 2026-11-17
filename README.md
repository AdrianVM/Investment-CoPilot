# Investment Co-Pilot

A stock assessment tool that is growing into a multi-user application. All planned work is tracked as OpenSpec changes in [`openspec/`](openspec/).

## Requirements

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or newer | `dotnet --version` |
| Node.js | 22.12 or newer (CI uses the version in [`.nvmrc`](.nvmrc)) | `node --version` |
| Docker with Compose | Compose v2 or newer | `docker compose version` |

If several .NET SDKs are installed in different places, make sure the `dotnet` on your `PATH` (and `DOTNET_ROOT`) is the one that has SDK 10: `dotnet --list-sdks` should list a `10.0.x` entry.

## Local environment

The local environment runs PostgreSQL and a development-only sign-in server (Keycloak) in Docker. Start it from the repository root:

```bash
docker compose up -d --wait
```

`--wait` returns once both services report healthy. The first start downloads the images and can take a few minutes.

| Service | Address | Notes |
|---|---|---|
| PostgreSQL 17 | `localhost:5433` | Database `investment_copilot`, user `copilot`, password `copilot`. Port 5433 avoids clashing with a locally installed Postgres. |
| Keycloak | <http://localhost:8080> | Realm `investment-copilot`. Admin console login: `admin` / `admin`. |

The realm is imported from [`dev/keycloak/investment-copilot-realm.json`](dev/keycloak/investment-copilot-realm.json) and contains two test users, so you can check that each user sees only their own data:

| Username | Password | Email |
|---|---|---|
| `alice` | `alice` | alice@example.com |
| `bob` | `bob` | bob@example.com |

All of these credentials are for local development only.

To stop the environment, keeping the database:

```bash
docker compose down
```

To also delete the database and start from scratch, add `-v`. Keycloak keeps no data outside its container, so the realm is re-imported whenever the container is recreated, and changes made in the Keycloak admin console are lost.

## Running the app

With the local environment running, start the API and the web app in two terminals.

API (listens on <http://localhost:5080>, and creates the database tables on first start):

```bash
dotnet tool restore
dotnet run --project backend/src/Host
```

Web app (listens on <http://localhost:5173> and forwards `/api` requests to the API):

```bash
cd frontend
npm ci
npm run dev
```

Open <http://localhost:5173>, choose **Sign in**, and sign in as `alice` or `bob`. The app shows the signed-in user's profile and a stock lookup.

The stock lookup uses made-up sample data for now (no real market data provider is connected). These symbols exist:

| Symbol | What it shows |
|---|---|
| `LYNCH` | Meets every Peter Lynch criterion |
| `BIGCO` | Fails the criteria: large, expensive, heavily indebted |
| `SPARSE` | Several metrics not available |
| `LOSSCO` | Loss-making, so no P/E |
| `EUROCO` | Listed in euros |

Any other valid symbol shows "No stock found".

## Tests

```bash
cd backend && dotnet test          # unit, architecture and integration tests (needs Docker running)
cd frontend && npm run lint && npm run typecheck && npm test
```

## Generated API client

The frontend calls the API through a typed client generated from the API's OpenAPI document. Building the backend writes [`backend/openapi.json`](backend/openapi.json). After changing an endpoint, regenerate the client and commit both:

```bash
dotnet build backend/src/Host
cd frontend && npm run generate:api
```

`npm run check:api` (also run in CI) fails if either file is out of date. Never edit `frontend/src/api/generated/` by hand.

## Building the web app

`npm run build` needs the production sign-in provider settings, `VITE_OIDC_AUTHORITY` and `VITE_OIDC_CLIENT_ID`, which are not chosen yet, so it fails until they are set. To build with the local development settings instead, run `npm run build:local`, then `npm run preview` to serve the result. The built app enforces a strict Content Security Policy, which only allows the configured sign-in provider and API.

More detail on the backend's structure and rules is in [`backend/README.md`](backend/README.md).
