# Design

## Context

`docker-compose.yml` runs Postgres 17 (host port 5433) and Keycloak 26.4 in dev mode (port 8080, `KC_HOSTNAME=http://localhost:8080`, realm imported from `dev/keycloak/`). The API runs with `dotnet run` on port 5080. In Development it applies migrations at startup and reads its connection string and `Auth` settings from `appsettings.Development.json`. The web app runs with `npm run dev` on port 5173, where Vite proxies `/api` to 5080. `npm run build:local` produces a static build with the local sign-in settings from `frontend/.env.development` and the Content Security Policy as a `<meta>` tag. The Keycloak web client only allows redirects to `http://localhost:5173/*`. The API exposes `/health/live` and `/health/ready` without authentication. See proposal.md for motivation and `specs/local-environment/spec.md` for the required behaviour.

## Goals / Non-Goals

**Goals:**
- The four containers behave like the IDE workflow: same addresses, same sign-in, same features.
- The images are production-shaped (multi-stage, runtime-only, configured by environment), so later deployment work starts from them.

**Non-Goals:**
- Hot reload or debugging inside containers.
- HTTPS, production hosting, or a production sign-in provider.

## Decisions

### API image (`backend/Dockerfile`)

A multi-stage build with `backend/` as the build context:
1. `mcr.microsoft.com/dotnet/sdk:10.0` copies `global.json`, `Directory.Build.props`, `Directory.Packages.props` and the `src/` project files first, and restores `src/Host` only, so the restore layer is cached until dependencies change. It then copies `src/` and runs `dotnet publish src/Host -c Release --no-restore -p:OpenApiGenerateDocuments=false`. The build-time OpenAPI generation is skipped: it starts the host and would rewrite `openapi.json` inside the container for nothing.
2. `mcr.microsoft.com/dotnet/aspnet:10.0` holds the published output only, runs as the image's non-root `app` user, and listens on the image default port 8080.

`backend/.dockerignore` excludes `bin/`, `obj/`, `tests/`, `openapi.json` and `src/Host/appsettings.Development.json`. That file holds the local database password, so leaving it out keeps credentials out of the image (spec: Local credentials stay local). An image started without settings therefore fails startup validation, which names the missing connection string and `Auth` settings. That validation already exists.

Alternative considered: building in a container from a pre-built `dotnet publish` on the host. Rejected because it needs the right SDK on the host, which is the problem this change removes.

### Web image (`frontend/Dockerfile`, `frontend/nginx.conf`)

1. A `node:26-alpine` stage, matching `.nvmrc`, runs `npm ci --ignore-scripts` and then `npm run build:local`. `--ignore-scripts` skips the `postinstall` step that installs the isolated API client generator, which the build does not need; the app's dependencies have no install scripts. The build reads `frontend/.env.development` (non-secret local sign-in settings), so the CSP allows `http://localhost:8080` exactly as a local build does.
2. An `nginx:1.28-alpine` stage serves `dist/` with `try_files $uri /index.html`, so `/callback`, `/silent-callback` and future routes load the app (spec: Reloading a sign-in page). `location /api/` proxies to `http://api:8080`, keeping requests same-origin (spec: The containerised web app keeps its security policy). Hashed assets under `/assets/` are cached for a long time; `index.html` is not cached.

Alternative considered: serving the build from the API with static files. Rejected because it couples the two deployables and differs from how the web app would be hosted.

### Compose services

```yaml
api:
  build: ./backend
  environment:
    ASPNETCORE_ENVIRONMENT: Development        # migrations at startup, /openapi/v1.json
    ConnectionStrings__Database: Host=postgres;Port=5432;Database=investment_copilot;Username=copilot;Password=copilot
    Auth__Authority: http://localhost:8080/realms/investment-copilot
    Auth__MetadataAddress: http://keycloak:8080/realms/investment-copilot/.well-known/openid-configuration
    Auth__Audience: investment-copilot-api
    Auth__RequireHttpsMetadata: "false"
  ports: ["5080:8080"]
  depends_on: { postgres: { condition: service_healthy }, keycloak: { condition: service_healthy } }
  healthcheck: bash /dev/tcp request to /health/ready (the runtime image has no curl)
web:
  build: ./frontend
  ports: ["5173:80"]
  depends_on: { api: { condition: service_healthy } }
  healthcheck: wget -q --spider http://localhost/ (busybox wget is in the alpine image)
```

Infrastructure-only mode needs no profiles: naming the services (`docker compose up postgres keycloak`) starts just those, and the API and web app images are not built (spec: Infrastructure can run on its own). Alternative considered: a Compose profile for the app. Rejected because the user chose "everything by default".

### Reaching Keycloak from inside the API container

The browser talks to Keycloak at `localhost:8080`, so tokens carry `iss = http://localhost:8080/realms/investment-copilot`. Inside the API container, `localhost` is the container itself. Two settings resolve this:

- **Keycloak:** `KC_HOSTNAME_BACKCHANNEL_DYNAMIC: "true"` (alongside the existing `KC_HOSTNAME`). The discovery document fetched through `http://keycloak:8080` then keeps the public issuer (`localhost:8080`) but advertises back-channel URLs such as `jwks_uri` on `keycloak:8080`, which the API can reach.
- **API:** a new optional `Auth:MetadataAddress` setting, mapped to `JwtBearerOptions.MetadataAddress`. The API downloads discovery and keys from that address, but still validates `iss` against the issuer in the discovery document and `Authority`. When the setting is unset, metadata comes from `Authority` as today. This is standard OIDC configuration, not Keycloak-specific (spec user-identity: Access tokens are validated against the configured provider).

Alternatives considered:
- `extra_hosts` mapping `localhost` to the host. Rejected because it breaks the container's own loopback.
- Running the API with `network_mode: host`. Rejected because it doesn't work on Docker Desktop for macOS and Windows.

The back-channel setting does not change anything for the IDE workflow, where everything uses `localhost:8080`.

### CI

A new `images` job runs `docker compose build api web`, so a broken Dockerfile fails the pull request. It does not start the stack; the end-to-end check is done by hand (tasks). Alternative considered: starting the stack in CI and running a browser test. Deferred, because Keycloak startup alone adds about a minute, and no browser test suite exists yet.

## Risks / Trade-offs

- [Ports 5080 and 5173 clash when the containers and the IDE workflow run at the same time] → The README says to use one or the other; `docker compose up postgres keycloak` is the IDE mode.
- [Building the images on first run takes several minutes] → Dependency restore layers are cached; later builds after code-only changes reuse them.
- [`KC_HOSTNAME_BACKCHANNEL_DYNAMIC` behaviour depends on the Keycloak version] → Keycloak is pinned to 26.4, and the end-to-end task verifies sign-in against the containerised API.
- [Local credentials in `docker-compose.yml`] → They already lived there for Postgres and Keycloak; the file is marked local-only, and the images contain none.
- [Known issue, predating this change: the realm file does not pin the test users' IDs, and Keycloak keeps no data, so each recreation of the Keycloak container (for example `docker compose down` then `up`) gives alice and bob new subject IDs, and the API creates new local users for them. Data they owned would no longer be theirs] → Deferred to a separate change by decision; the fix is fixed `id` values for the users in `dev/keycloak/investment-copilot-realm.json`.
- [The web image bakes in the local sign-in settings] → It is a local image by design; a production web image will need its own build arguments when deployment is planned.

## Migration Plan

No data migration. Existing local databases (the `postgres-data` volume) are reused, because the containerised API applies pending migrations at startup like `dotnet run` does. To return to the old workflow, run `docker compose up postgres keycloak`.
