# Investment Co-Pilot

A stock assessment tool that is growing into a multi-user application. All planned work is tracked as OpenSpec changes in [`openspec/`](openspec/).

## Quick start

The whole app runs in Docker, so all you need is Docker with Compose v2 (`docker compose version`). From the repository root:

```bash
docker compose up --build --wait
```

This builds the API and web app images from your working tree, then starts the database, the sign-in server, the API and the web app. `--wait` returns once all four are healthy. The first run downloads and builds images and takes a few minutes; later runs reuse the cache.

Open <http://localhost:5173>, choose **Sign in**, and sign in as `alice` / `alice` or `bob` / `bob`. The app shows the signed-in user's profile and a stock lookup.

Below a stock's figures, the app checks it against Peter Lynch's six rules of thumb (see [`docs/peter-lynch-stock-criteria.jpeg`](docs/peter-lynch-stock-criteria.jpeg)). Each criterion is met, not met, or cannot be assessed when data is missing. This is for information only, not investment advice.

The stock lookup uses made-up sample data and sample exchange rates for now (no real market data provider is connected). These symbols exist:

| Symbol | What it shows | Peter Lynch checklist |
|---|---|---|
| `LYNCH` | A stock that fits the criteria | Meets 6 of 6 |
| `BIGCO` | Large, expensive, heavily indebted | Meets 0 of 6 |
| `SPARSE` | Several metrics not available | Meets 2 of 6; 3 could not be assessed |
| `LOSSCO` | Loss-making, so no P/E | Meets 4 of 6; 2 could not be assessed |
| `EUROCO` | Listed in euros; market cap converted to dollars | Meets 6 of 6 |

Any other valid symbol shows "No stock found".

| Service | Address | Notes |
|---|---|---|
| Web app | <http://localhost:5173> | nginx serving the built app; forwards `/api` to the API. |
| API | <http://localhost:5080> | Health: `/health/ready`. In Development it applies database migrations at startup. |
| Keycloak | <http://localhost:8080> | Development-only sign-in server. Realm `investment-copilot`. Admin console login: `admin` / `admin`. |
| PostgreSQL 17 | `localhost:5433` | Database `investment_copilot`, user `copilot`, password `copilot`. Port 5433 avoids clashing with a locally installed Postgres. |

The realm is imported from [`dev/keycloak/investment-copilot-realm.json`](dev/keycloak/investment-copilot-realm.json) and contains two test users, so you can check that each user sees only their own data:

| Username | Password | Email |
|---|---|---|
| `alice` | `alice` | alice@example.com |
| `bob` | `bob` | bob@example.com |

All of these credentials are for local development only. They live in `docker-compose.yml` and the local settings files; the images contain none.

After changing code, rebuild and restart the changed containers:

```bash
docker compose up --build --wait
```

To stop everything, keeping the database:

```bash
docker compose down
```

To also delete the database and start from scratch, add `-v`. Keycloak keeps no data outside its container, so the realm is re-imported whenever the container is recreated, and changes made in the Keycloak admin console are lost.

## Developing

For hot reload and debugging, run only the database and sign-in server in Docker, and the API and web app on your machine. This needs:

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or newer | `dotnet --version` |
| Node.js | 22.12 or newer (CI uses the version in [`.nvmrc`](.nvmrc); `nvm use` selects it) | `node --version` |

If several .NET SDKs are installed in different places, make sure the `dotnet` on your `PATH` (and `DOTNET_ROOT`) is the one that has SDK 10: `dotnet --list-sdks` should list a `10.0.x` entry.

Start only the infrastructure:

```bash
docker compose up postgres keycloak --wait
```

The containerised API and web app use the same ports (5080 and 5173) as the ones you run yourself, so stop them first if they are running (`docker compose stop api web`).

Then start the API and the web app in two terminals.

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

Open <http://localhost:5173> and sign in as above.

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
