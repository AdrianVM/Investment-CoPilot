# Tasks

## 1. API: metadata address setting

- [x] 1.1 Add the optional `Auth:MetadataAddress` setting to `AuthOptions`, mapped to `JwtBearerOptions.MetadataAddress` when set, and document it in `backend/README.md`; verify with an integration test that tokens are still accepted when it is unset, and that with it set the issuer is still validated (a token from another issuer gets HTTP 401)

## 2. Images

- [x] 2.1 Add `backend/Dockerfile` (multi-stage SDK build of `src/Host` with a cached restore layer, OpenAPI generation off, ASP.NET runtime image, non-root) and `backend/.dockerignore` (excluding `bin/`, `obj/`, `tests/`, `openapi.json` and `appsettings.Development.json`); verify `docker build backend` succeeds, the image contains no `appsettings.Development.json`, and running it with no settings exits with startup validation errors naming the connection string and `Auth` settings
- [x] 2.2 Add `frontend/Dockerfile` (`node:26-alpine` running `npm ci --ignore-scripts` and `npm run build:local`, then nginx), `frontend/nginx.conf` (SPA fallback to `index.html`, `/api/` proxied to `http://api:8080`, long cache for `/assets/`, no cache for `index.html`) and `frontend/.dockerignore`; verify `docker build frontend` succeeds and that `/`, `/callback` and an asset path are served by `docker run`

## 3. Compose

- [x] 3.1 Add the `api` and `web` services to `docker-compose.yml` (environment from the design, ports 5080 and 5173, `depends_on` with health conditions, health checks) and `KC_HOSTNAME_BACKCHANNEL_DYNAMIC` to Keycloak; verify that `docker compose up --build --wait` returns with all four services healthy, `http://localhost:5080/health/ready` answers, and `http://localhost:5173/api/me` without a token gives HTTP 401 through the nginx proxy
- [x] 3.2 Verify that `docker compose up postgres keycloak` starts only those two (no images built), and that `dotnet run` and `npm run dev` still sign in and work against them as before

## 4. CI and documentation

- [x] 4.1 Add an `images` job to `.github/workflows/ci.yml` that runs `docker compose build api web`; verify the same command passes locally
- [x] 4.2 Update `README.md`: the one-command start (`docker compose up --build --wait`), addresses, rebuilding after changes, `docker compose down` vs `down -v`, the infrastructure-only IDE workflow, and the port clash between the two; verify the documented commands work from a clean clone

## 5. End-to-end verification

- [x] 5.1 With only the containers running, check in a headless browser: sign in as `alice`, see the profile, look up `LYNCH` and `EUROCO` (figures and checklist), reload `/callback` directly, and confirm there are no CSP or console errors; then run `docker compose down` and `up` again and confirm alice's user still exists, and that `down -v` resets it
- [x] 5.2 Run the full backend and frontend test suites (`dotnet test`, `dotnet format --verify-no-changes`, `npm run lint`, `npm run typecheck`, `npm test`, `npm run build:local`, `check:api`) and verify all pass
