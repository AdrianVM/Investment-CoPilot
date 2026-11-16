#!/usr/bin/env bash
# Fails if backend/openapi.json or the generated frontend client is out of date with the backend code.
# Regenerates both, then compares them with what git has (the index, which equals HEAD on a clean checkout).
set -euo pipefail
cd "$(dirname "$0")/.."

GENERATED=(backend/openapi.json frontend/src/api/generated)

echo "Regenerating the OpenAPI document..."
dotnet build backend/src/Host --nologo --verbosity quiet

echo "Regenerating the API client..."
npm --prefix frontend run --silent generate:api >/dev/null

untracked=$(git ls-files --others --exclude-standard -- "${GENERATED[@]}")
if ! git diff --quiet -- "${GENERATED[@]}" || [ -n "$untracked" ]; then
  echo
  echo "The generated API files are out of date:"
  git diff --stat -- "${GENERATED[@]}"
  [ -z "$untracked" ] || printf '  new file: %s\n' $untracked
  echo
  echo "Run 'npm run generate:api' in frontend/ after building the backend, and commit the result."
  exit 1
fi

echo "Generated API files are up to date."
