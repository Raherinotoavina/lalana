# Phase 02 — API and infrastructure foundation

## Status

In progress — on branch `feat/02-api-and-infrastructure-foundation`. Everything is
built and tested locally; deployment waits until a VPS is rented.

## Objective

Stand up the ASP.NET Core API with its layered solution, database access, token
validation, OpenAPI client generation, tests, and a Docker Compose stack that runs
locally today and on a small VPS later.

## Conversation context and prior decisions

- The owner chose a C# backend from V1 (2026-11-09) to avoid rewriting business logic
  later.
- 2026-11-09: no Supabase. The backend is self-hosted for the lowest cost: PostgreSQL +
  PostGIS in Docker, ASP.NET Core Identity for accounts (Phase 03), photos on the host
  behind `IFileStorage` (Phase 04). Development is local only; a small VPS (about
  4–6 € a month) hosts staging and the beta later. See
  [architecture](../architecture.md).
- The API must stay always-on (SignalR, background jobs): no free tier that sleeps.

## Scope

- Solution `api/` with `Lalana.Domain`, `Lalana.Application`, `Lalana.Infrastructure`,
  `Lalana.Api`, and the three test projects (see architecture).
- Current .NET LTS (.NET 10), nullable enabled, warnings as errors, central package
  management, `dotnet test` on Microsoft.Testing.Platform with xUnit v3.
- EF Core + Npgsql + NetTopologySuite, schema `app`, PostGIS enabled, first migration.
- JWT bearer authentication for the API's own access tokens (issuer, audience,
  lifetime, asymmetric signing key from configuration); an authenticated `GET /me`
  returning the caller's id. Issuing tokens comes with Phase 03.
- `GET /health` (liveness) and `GET /health/ready` (database reachable).
- OpenAPI document generated at build time and committed; the mobile app generates
  its typed client from it (`apps/mobile/src/services/api/`).
- Consistent error format (RFC 9457 problem details), request logging with a
  correlation id, no personal data in logs.
- Integration tests with `WebApplicationFactory` and Testcontainers (PostGIS image).
- Coverage ≥ 80 % (lines and branches) for the API, enforced in CI.
- Dockerfile (small runtime image) and a Docker Compose file: PostgreSQL + PostGIS and
  the API for local use, reusable on the VPS with Caddy for HTTPS.
- Environments: local (Docker Compose) now; staging on the VPS later.

## Out of scope

- Feature endpoints (from Phase 03 on).
- Renting the VPS, the staging deployment and backups: done when a server is needed
  (before the closed beta at the latest).
- Production deployment (Phase 12).

## Dependencies

- Docker on the developer's machine (and on CI runners for the integration tests).

## Implementation tasks

- [ ] Create the solution, projects, and shared build settings.
- [ ] Add EF Core, the `app` schema, PostGIS, and the first migration.
- [ ] Add JWT validation for the API's own tokens and `GET /me`.
- [ ] Add health endpoints, problem details, logging, and correlation ids.
- [ ] Publish OpenAPI at build time and wire client generation in the mobile app.
- [ ] Add unit and integration test projects with Testcontainers.
- [ ] Add the API CI job (format, build, tests, coverage gate, OpenAPI drift, image
  build) and the C# pre-commit checks.
- [ ] Add the Dockerfile and the local Docker Compose stack; document secrets and
  environments.
- [ ] Later, when the VPS exists: Compose with Caddy, deploy from `main`, migrations in
  the deploy step, encrypted off-site backups with a tested restore.

## Acceptance criteria

- [ ] `GET /me` returns 401 without a token, 401 with an expired token or a wrong
  issuer or audience, and the user id with a valid token.
- [ ] `GET /health/ready` reports the local database as reachable, and not ready when
  it is down.
- [ ] The mobile app calls `GET /me` through the generated client.
- [ ] Format, build, tests, and coverage ≥ 80 % pass locally and in CI.
- [ ] `docker compose up` starts the database and the API locally.
- [ ] Later: a merge to `main` deploys staging on the VPS and applies migrations.

## Test strategy

Integration tests against a real PostGIS container: authentication (valid, expired,
wrong issuer, wrong audience), health, error format, correlation id. Unit tests for
the domain and application helpers.

## Risks and security considerations

- The token signing key and the database connection string are secrets: environment
  or secret files on the host, never in git; a development key is generated locally.
- PostgreSQL is never published on a public port; only the API reaches it.
- A single VPS is a single point of failure: off-site backups and a restore test are
  required before real users.
- Latency: measure from Antananarivo before choosing the VPS region.

## Notes and decisions

- 2026-11-09: Supabase removed from the plan; JWTs are the API's own (Phase 03 issues
  them).
