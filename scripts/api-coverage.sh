#!/bin/sh
# API tests with coverage: runs every test project, merges their Cobertura reports with
# ReportGenerator, then fails below 80 % lines or branches. Excluded: test code, EF Core
# migrations and source-generated code under obj/ (regex, logging, OpenAPI generators).
set -eu
cd "$(dirname "$0")/../api"

rm -rf TestResults
dotnet test --solution Lalana.slnx --coverage --coverage-output-format cobertura --results-directory TestResults "$@"
dotnet tool run reportgenerator \
  "-reports:TestResults/*.cobertura.xml" \
  "-targetdir:TestResults/coverage" \
  "-reporttypes:JsonSummary;TextSummary;Html;MarkdownSummaryGithub" \
  "-assemblyfilters:+Lalana.*;-Lalana.*.Tests;-Lalana.*.IntegrationTests" \
  "-classfilters:-Lalana.Infrastructure.Persistence.Migrations.*" \
  "-filefilters:-**/obj/**"
cat TestResults/coverage/Summary.txt
node ../scripts/coverage-gate.mjs TestResults/coverage/Summary.json API 80
