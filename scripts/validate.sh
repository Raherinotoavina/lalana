#!/bin/sh
# Complete validation suite, run by the pre-push hook and mirrored by CI.
set -eu
cd "$(dirname "$0")/.."

echo "› Repository scripts: tests with coverage"
npm run test:scripts

echo "› Mobile: lint, typecheck, format, tests with coverage"
npm --prefix apps/mobile run validate
node scripts/coverage-gate.mjs apps/mobile/coverage/coverage-summary.json Mobile

echo "› API: format, build, OpenAPI drift, tests with coverage (needs Docker)"
dotnet tool restore --tool-manifest api/dotnet-tools.json >/dev/null
dotnet format api/Lalana.slnx --verify-no-changes
dotnet build api/Lalana.slnx --warnaserror
git diff --exit-code -- api/openapi || {
  echo "api/openapi is out of date: commit the regenerated document." >&2
  exit 1
}
sh scripts/api-coverage.sh
