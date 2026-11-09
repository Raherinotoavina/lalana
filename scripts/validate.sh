#!/bin/sh
# Complete validation suite, run by the pre-push hook and mirrored by CI.
set -eu
cd "$(dirname "$0")/.."

echo "› Repository scripts: tests with coverage"
npm run test:scripts

echo "› Mobile: lint, typecheck, format, tests with coverage"
npm --prefix apps/mobile run validate
node scripts/coverage-gate.mjs apps/mobile/coverage/coverage-summary.json Mobile

if [ -d api ] && ls api/*.sln* >/dev/null 2>&1; then
  echo "› API: format, build, tests with coverage"
  dotnet format api --verify-no-changes
  dotnet build api --warnaserror
  dotnet test api
fi
