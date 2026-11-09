# Phase 02 — API and infrastructure foundation

## Status

Not started

## Objective

Stand up the ASP.NET Core API with its layered solution, database access, JWT
validation, OpenAPI client generation, tests, and an automatic staging deployment.

## Conversation context and prior decisions

- The owner chose a C# backend from V1 (2026-11-09) to avoid rewriting business logic
  later. Supabase is kept for Postgres + PostGIS, Auth, and Storage.
- Deployment target: Docker image on a European PaaS near the Supabase region.

## Scope

- Solution `api/` with `Lalana.Domain`, `Lalana.Application`, `Lalana.Infrastructure`,
  `Lalana.Api`, and the three test projects (see architecture).
- Current .NET LTS, nullable enabled, warnings as errors, central package management.
- EF Core + Npgsql + NetTopologySuite against Supabase Postgres, schema `app`, first
  migration, PostGIS enabled.
- JWT bearer authentication validating Supabase tokens (issuer, audience, JWKS keys);
  an authenticated `GET /me` endpoint returning the caller's id.
- `GET /health` (liveness) and `GET /health/ready` (database reachable).
- OpenAPI document; the mobile app generates its typed client from it
  (`apps/mobile/src/services/api/`).
- Consistent error format (RFC 9457 problem details), request logging with a
  correlation id, no personal data in logs.
- Integration tests with `WebApplicationFactory` and Testcontainers (PostGIS image).
- Coverage ≥ 80 % (lines and branches) enforced in CI.
- Dockerfile (small runtime image), staging deployment from `main` through GitHub
  Actions, EF Core migrations applied by the deploy step.
- Environments: local (Docker Postgres), staging, production (later).

## Out of scope

- Feature endpoints (from Phase 03 on).
- Production deployment (Phase 12).

## Dependencies

- A hosted Supabase project (staging) and a PaaS account.

## Implementation tasks

- [ ] Create the solution, projects, and shared build settings.
- [ ] Add EF Core, the `app` schema, PostGIS, and the first migration.
- [ ] Add Supabase JWT validation and `GET /me`.
- [ ] Add health endpoints, problem details, logging, and correlation ids.
- [ ] Publish OpenAPI and wire client generation in the mobile app.
- [ ] Add unit and integration test projects with Testcontainers.
- [ ] Add the API CI job (format, build, tests, coverage threshold, image build).
- [ ] Deploy staging automatically and document secrets and environments.

## Acceptance criteria

- [ ] `GET /me` returns 401 without a token and the user id with a valid Supabase token.
- [ ] `GET /health/ready` reports the staging database as reachable.
- [ ] The mobile app calls `GET /me` through the generated client.
- [ ] Format, build, tests, and coverage ≥ 80 % pass in CI; a merge to `main`
  deploys staging and applies migrations.

## Test strategy

Integration tests against a real PostGIS container: authentication (valid, expired,
wrong issuer), health, error format. Unit tests for any helper in `Domain`.

## Risks and security considerations

- The database connection string and signing configuration are secrets in the PaaS
  secret store only.
- The `app` schema must not be granted to Supabase's `anon` / `authenticated` roles.
- Latency: measure from Antananarivo before fixing the API and database regions.

## Notes and decisions

Verify the Supabase JWT signing method (asymmetric keys and JWKS endpoint) in the
current Supabase docs before implementing validation.
