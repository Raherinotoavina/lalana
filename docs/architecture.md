# Architecture

Status: agreed for V1 on 2026-11-09. Working product name: "lalana" (placeholder).

## Decision

V1 ships with a dedicated **C# / ASP.NET Core** backend from the start, so the
business logic is never rewritten.

**Self-hosted, lowest cost (decided 2026-11-09, replacing Supabase).** The whole
backend runs in Docker and depends on no backend-as-a-service: the API,
**PostgreSQL + PostGIS**, **ASP.NET Core Identity** for accounts, and file storage for
photos. Development runs entirely on the developer's machine (cost: 0). When staging
or the beta needs a server, the same Docker Compose stack goes on **one small VPS**
(about 4–6 € a month) with encrypted off-site backups on an object-storage free tier.

Why not the alternatives:

- **Supabase**: once accounts move into the API, it would only host the database, as
  one more vendor; the free tier pauses after a week of inactivity, and the API still
  needs a host of its own.
- **Managed platforms** (Railway, Render, Azure App Service…): simpler to operate but
  2 to 5 times the cost of a VPS for the same load.
- **Free tiers that sleep or scale to zero**: they break the SignalR chat and the
  background jobs (outbox, expirations), and the first request after a pause takes
  seconds, which is unacceptable on 3G.

The code stays portable: moving the database to a managed Postgres, or the API to a
managed platform, later only changes configuration and the deploy step.

Earlier alternatives for the backend language: Node/TypeScript (shared language, more
assembly), Go (cheapest to run, more hand-written code), Java (heavier to host), Rust
(too slow to develop for this product).

## Stack

| Layer | Choice |
|---|---|
| Mobile app | Expo (React Native, TypeScript), Expo Router, Android first, iOS supported |
| Mobile server state | TanStack Query (cache, retries, loading and error states, invalidation) over the generated API client |
| API | ASP.NET Core on the current .NET LTS (verify at Phase 02), minimal APIs, OpenAPI |
| Data access | EF Core + Npgsql + NetTopologySuite (PostGIS) |
| Database | PostgreSQL + PostGIS in Docker (local now, the VPS later), app tables in schema `app`, never reachable from the internet |
| Auth | ASP.NET Core Identity in the API (users in schema `app`): sign-up with a mobile number and/or an e-mail (at least one, each confirmed by a code) and a password; sign-in with either one + password; password reset by SMS or e-mail code; Google (ID token verified by the API). The API issues short-lived JWT access tokens and rotating refresh tokens |
| SMS | SMS provider for Madagascar numbers (+261), called by the API behind a port; the cheapest reliable one, to be chosen in Phase 03 |
| E-mail | Transactional e-mail service with a free tier (Brevo, Resend…), behind a port, to be chosen in Phase 03 |
| Files | `IFileStorage` port; first implementation on a disk volume of the host, served through short-lived signed URLs issued by the API; images resized and compressed on upload; can move to S3-compatible storage later without code changes |
| Realtime | SignalR hub in the API (chat, booking status) |
| Background work | Hosted services: transactional outbox, expirations, push dispatch |
| Push | `expo-notifications` on device; the API calls the Expo push service |
| Payments | Local provider for Madagascar, to be decided (Phase 10) |
| Hosting | Now: local only, Docker Compose. Later (staging, beta, V1): one small VPS running Docker Compose (API, PostgreSQL + PostGIS, Caddy for HTTPS), encrypted daily backups to an object-storage free tier (Cloudflare R2 or Backblaze B2) with a tested restore, free uptime monitoring |
| CI/CD | GitHub Actions: mobile checks, API checks, image build; deploy to the VPS once it exists |

## Repository layout

```
apps/mobile/            Expo app
api/
  src/Lalana.Domain/          Entities, value objects, business rules (no I/O)
  src/Lalana.Application/     Use cases, ports (interfaces), DTOs
  src/Lalana.Infrastructure/  EF Core, Identity stores, file storage, SMS, e-mail, payment, push adapters
  src/Lalana.Api/             HTTP endpoints, auth, SignalR, hosted services
  tests/Lalana.Domain.Tests/
  tests/Lalana.Application.Tests/
  tests/Lalana.Api.IntegrationTests/   WebApplicationFactory + Testcontainers (PostGIS)
docs/
```

## Rules

1. **All business rules live in the API**, in `Domain` and `Application`. The mobile
   app never decides a price, a commission, a seat count, or a status transition.
2. **The mobile app talks to the API only.** The database listens on the private
   Docker network, never on a public port; nothing but the API reaches it.
3. **Generated API client, read through TanStack Query.** The API publishes an OpenAPI
   document; the mobile app generates its TypeScript client and types from it.
   `src/services/<domain>.ts` wraps the generated client; `src/features/<domain>` exposes
   TanStack Query hooks (`useQuery` / `useMutation`) over those services, and screens use
   only the hooks (decided 2026-11-09). No screen calls `fetch` or the client directly.
   Queries retry network and 5xx errors (not 4xx); mutations are never retried
   automatically.
4. **Identity is the ASP.NET Core Identity user id** (a GUID). The API issues and
   validates its own tokens: short-lived JWT access tokens (asymmetric signature,
   checked issuer, audience and lifetime) and refresh tokens stored hashed, rotated
   on use and revocable. The mobile app keeps them in `expo-secure-store`.
5. **Money is a `long` number of Ariary** (DB `bigint`), never a floating type. Every
   movement is an append-only ledger row; every provider call carries an idempotency
   key; every webhook is verified and processed idempotently.
6. **Side effects go through a transactional outbox.** A booking change and its
   notification events are written in the same transaction; a hosted service sends
   push notifications and SignalR events afterwards.
7. **Time is `timestamptz` (UTC)**, displayed in `Indian/Antananarivo`.
8. **Secrets stay server-side.** The mobile app holds only public configuration (the
   API URL). The token signing key, the database connection string and provider keys
   live in environment or secret files on the host (never in git) and in CI secrets.
9. **Migrations are EF Core migrations**, applied by the deploy step (and by the
   integration tests), never by hand on staging or production.
10. **Every code change is tested, and each project keeps its own coverage at or above
    80 %** (statements, branches, functions and lines for the mobile app; lines and
    branches for the API). The threshold is measured per project, never as an average
    across projects, and is enforced in each project's test configuration and in CI.

## Domain model (first draft)

| Entity | Purpose |
|---|---|
| `Profile` | Public profile (name, photo, bio, rating aggregates), 1:1 with the Auth user |
| `Vehicle`, `VehiclePhoto` | Type (car, motorbike, scooter), brand and model, colour, photos |
| `Place` | Curated known place (neighbourhood, landmark) with a PostGIS point |
| `Trip`, `TripStop` | Driver offer; ordered stops with estimated time and price to the next stop |
| `RideRequest` | Passenger post: from, to, time window, seats, optional budget |
| `Offer` | Driver proposal answering a ride request |
| `Booking` | Seats on a trip between stop `FromIndex` and `ToIndex`, with status |
| `Conversation`, `Message` | One-to-one chat, optionally linked to a trip or request |
| `Notification`, `PushToken` | In-app notification feed and device tokens |
| `Review` | Rating 1–5, tags, comment, after a completed booking |
| `LedgerEntry` | Append-only money movement (commission, top-up, refund) |
| `OutboxMessage` | Pending side effect (push, realtime event) |

### Booking state machine

```
Requested ──driver accepts──▶ Accepted ──passenger confirms + pays──▶ Confirmed ──trip ends──▶ Completed
    │                            │
    ├─ driver refuses ▶ Refused  ├─ confirmation timeout ▶ Expired
    └─ timeout ▶ Expired         └─ Cancelled (rules TBD)
```

An offer on a ride request follows the same machine: the driver's offer plays the
"Accepted" role; the passenger's acceptance plus payment is the confirmation.

### Seat capacity per segment

A trip with stops `0..n` has segments `0..n-1`. A booking from `a` to `b` occupies
segments `a..b-1`. For every segment, the seats of `Accepted` and `Confirmed`
bookings must not exceed the trip's seats. Accepting a booking runs in one
transaction that locks the trip row (`SELECT … FOR UPDATE`), checks every segment,
then changes the status. The rule is a pure function in `Domain` with unit tests, and
the locking is covered by a concurrent integration test.

## Open decisions

- Payment provider for Madagascar (Mobile Money and/or cards) and whether commissions
  come from an in-app balance (recommended: card fees would exceed small commissions).
- Commission amounts, and when the driver's commission is taken (recommended: when the
  booking becomes `Accepted`, or when the passenger accepts a driver's offer).
- Confirmation timeout and cancellation rules.
- Whether the fare itself is paid in cash to the driver or through the app.
- SMS provider for sign-in codes (delivery to Malagasy operators, price per SMS, fraud
  protection).
- Transactional e-mail provider (free tier, deliverability, French templates).
- VPS provider and region when staging is needed (latency check from Antananarivo,
  price), and the backup target.
- Final product name.
