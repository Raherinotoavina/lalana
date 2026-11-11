# Implementation phases

This directory is the source of truth for the V1 implementation phases. The order
reflects dependencies, not delivery dates. A phase stays in progress until every
acceptance criterion is verified, including manual checks on a native build; a merged
pull request alone does not complete it.

Design board: Claude Design canvas "Covoiturage — Maquette V1"
(https://claude.ai/artifact/AeiqLeQhtQeiZi2e6Pevty). Palette "Électrique"
(tokens in `apps/mobile/src/theme/tokens.ts`).
Architecture, stack, and mandatory rules: [../architecture.md](../architecture.md).

V1 uses an Expo mobile app and a C# / ASP.NET Core API that owns every business rule
and the accounts (ASP.NET Core Identity), with PostgreSQL + PostGIS and photo storage,
all self-hosted in Docker: locally during development, on one small VPS later
(decided 2026-11-09 for the lowest cost; no Supabase).

## V1 roadmap

1. [Repository and mobile foundation](./01-repository-and-mobile-foundation.md) — in progress.
2. [API and infrastructure foundation](./02-api-and-infrastructure-foundation.md) — in progress.
3. [Authentication](./03-authentication.md) — not started.
4. [Profiles and vehicles](./04-profiles-and-vehicles.md) — not started.
5. [Places and publishing](./05-places-and-publishing.md) — not started.
6. [Feed, search, and trip detail](./06-feed-search-and-trip-detail.md) — not started.
7. [Booking flow](./07-booking-flow.md) — not started.
8. [Notifications](./08-notifications.md) — not started.
9. [Messaging](./09-messaging.md) — not started.
10. [Payments and commissions](./10-payments-and-commissions.md) — research can start now; implementation blocked on the provider decision.
11. [Ratings and recommendations](./11-ratings.md) — not started.
12. [V1 stabilization and release](./12-v1-release.md) — not started.

Phases 01 and 02 can run in parallel. Phases 08 and 09 can run in parallel once
Phase 07's entities exist.

Post-V1 ideas live in the [feature backlog](../features/README.md).
