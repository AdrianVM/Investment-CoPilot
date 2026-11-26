# Proposal

## Why

Running the app locally takes three terminals and the right local toolchain: Docker for Postgres and Keycloak, the .NET 10 SDK for the API, and the Node version from `.nvmrc` for the web app. Mismatched SDK and Node versions have already caused failed builds. Packaging the API and the web app as containers makes the whole app start with one command on any machine with Docker, and gives later deployment work a tested pair of images.

## What Changes

- Add a container image for the **API**: built with the .NET 10 SDK and run on the ASP.NET Core 10 runtime image, configured only through environment variables.
- Add a container image for the **web app**: built with Node using the local sign-in settings (`build:local`) and served by nginx. nginx serves the single-page app, including its sign-in callback paths, and forwards `/api` to the API, so the browser keeps making same-origin requests under the existing Content Security Policy.
- Add both to `docker-compose.yml`, so `docker compose up` starts Postgres, Keycloak, the API and the web app. They stay on today's addresses: web app `http://localhost:5173`, API `http://localhost:5080`, Keycloak `http://localhost:8080`. Each service waits for what it depends on, and `docker compose up --wait` returns once all four are healthy. Code changes take effect with `docker compose up --build`.
- Keep the current workflow available: `docker compose up postgres keycloak` starts only the infrastructure, for running the API and the web app from an IDE.
- Let the API fetch the sign-in provider's metadata from a different address than the token issuer, through a new optional setting, `Auth:MetadataAddress`. Inside Compose the browser reaches Keycloak at `localhost:8080`, while the API container must reach it at `keycloak:8080`. This stays provider-agnostic, and with the setting unset nothing changes.
- Build both images in CI, so a broken Dockerfile fails the pull request.
- Update `README.md` with the one-command start, the infrastructure-only option, and how to rebuild and reset.

Out of scope: production images, hosting and deployment; HTTPS; hot reload inside containers; publishing images to a registry.

## Capabilities

### New Capabilities
- `local-environment`: Running the application locally: starting the complete app with one command, starting only the infrastructure, the addresses services are reachable on, start-up order and readiness, rebuilding from current source, keeping or resetting data, and the local-only nature of the credentials.

### Modified Capabilities
- None. Sign-in still validates tokens against the configured issuer and audience (`user-identity`). The new metadata address only changes where the signing keys are downloaded from.

## Impact

- New files: `backend/Dockerfile`, `backend/.dockerignore`, `frontend/Dockerfile`, `frontend/.dockerignore`, `frontend/nginx.conf`.
- Changed: `docker-compose.yml` (API and web services, Keycloak back-channel setting), `backend/src/Host/Authentication` (`Auth:MetadataAddress`), `.github/workflows/ci.yml` (image build job), `README.md` and `backend/README.md`.
- No application behaviour, API or database changes.
- Running the containers and the IDE workflow at the same time clashes on ports 5080 and 5173. The README documents this; use one or the other.
