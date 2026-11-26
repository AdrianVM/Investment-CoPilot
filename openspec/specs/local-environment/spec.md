# local-environment Specification

## Purpose

Defines how a developer runs Investment Co-Pilot on their own machine: the whole application with one command, or only its infrastructure, on fixed local addresses, built from the current source, with local-only credentials.

## Requirements

### Requirement: One command starts the complete application
Running `docker compose up` from the repository root SHALL start the database, the development sign-in provider, the API and the web app, needing only Docker on the machine. The web app SHALL be reachable at `http://localhost:5173`, the API at `http://localhost:5080`, and the sign-in provider at `http://localhost:8080`.

#### Scenario: Fresh machine
- **WHEN** a developer with only Docker installed runs `docker compose up --wait` in a fresh clone
- **THEN** the command returns once all four services are healthy, and opening `http://localhost:5173` shows the app

#### Scenario: Sign in and use the app
- **WHEN** the developer signs in as a local test user in the containerised web app and looks up `LYNCH`
- **THEN** the profile, the stock's figures and the Peter Lynch checklist are shown, exactly as when running the app outside containers

### Requirement: Services start in dependency order
Each service SHALL wait until the services it depends on are healthy: the API waits for the database and the sign-in provider, and the web app waits for the API. The API and web app SHALL report their own health so `docker compose up --wait` can wait for them.

#### Scenario: Database still starting
- **WHEN** the database is not yet accepting connections
- **THEN** the API container has not started, and starts once the database is healthy

### Requirement: Infrastructure can run on its own
Running `docker compose up postgres keycloak` SHALL start only the database and the sign-in provider, on the same addresses as before, so the API and web app can run outside containers.

#### Scenario: Working from an IDE
- **WHEN** a developer runs `docker compose up postgres keycloak` and then starts the API and web app with `dotnet run` and `npm run dev`
- **THEN** sign-in and all features work as they did before this change

### Requirement: Containers run the current source
The API and web app images SHALL be built from the repository's working tree. Running `docker compose up --build` SHALL rebuild them, so source changes take effect.

#### Scenario: After a code change
- **WHEN** a developer changes the web app's source and runs `docker compose up --build`
- **THEN** the served web app includes the change

### Requirement: Data is kept until explicitly reset
The database's data SHALL survive stopping and restarting the containers, and SHALL be removed only when the developer asks for it.

#### Scenario: Restart
- **WHEN** a developer runs `docker compose down` and then `docker compose up` again
- **THEN** the users and cached market data created before are still there

#### Scenario: Reset
- **WHEN** a developer runs `docker compose down -v`
- **THEN** the next start begins with an empty database

### Requirement: The containerised web app keeps its security policy
The containerised web app SHALL be served with the same Content Security Policy as a local build, and SHALL reach the API through the same origin, so it needs no cross-origin configuration.

#### Scenario: No policy violations
- **WHEN** a signed-in user uses the containerised web app
- **THEN** the browser reports no Content Security Policy violations, and API requests go to `http://localhost:5173/api/...`

#### Scenario: Reloading a sign-in page
- **WHEN** the browser loads `http://localhost:5173/callback` or another app path directly
- **THEN** the web app is served rather than a "not found" page

### Requirement: Local credentials stay local
The local environment's passwords, test users and settings SHALL be defined only in the Compose configuration and local settings files. The images SHALL contain no credentials, and the API image SHALL take all its settings from its environment.

#### Scenario: Inspecting the API image
- **WHEN** the API image is run without environment settings
- **THEN** it does not connect to the local database or sign-in provider, and fails at startup with a message naming the missing settings
