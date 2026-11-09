# Architecture

Status: agreed for V1 on 2026-11-09. Working product name: "lalana" (placeholder).

## Decision

V1 ships with a dedicated **C# / ASP.NET Core** backend from the start, so the
business logic is never rewritten. Supabase is kept only for what it does well and
would be costly to rebuild: managed **Postgres (+ PostGIS)**, **Auth** (phone or
e-mail with password, verification codes, password reset, Google sign-in), and **Storage** (photos).

Alternatives considered: Supabase-only V1 (faster, but business logic would be
rewritten later), Node/TypeScript (shared language, more assembly), Go (cheapest to
run, more hand-written code), Java (heavier to host), Rust (too slow to develop for
this product).

## Stack

| Layer | Choice |
|---|---|
| Mobile app | Expo (React Native, TypeScript), Expo Router, Android first, iOS supported |
| API | ASP.NET Core on the current .NET LTS (verify at Phase 02), minimal APIs, OpenAPI |
| Data access | EF Core + Npgsql + NetTopologySuite (PostGIS) |
| Database | Supabase-managed Postgres + PostGIS, app tables in schema `app` |
| Auth | Supabase Auth: sign-up with a mobile number and/or an e-mail (at least one, each confirmed by a code) and a password; sign-in with either one + password; password reset by SMS or e-mail code; Google. The API validates Supabase JWTs |
| SMS | An SMS provider supported by Supabase Auth, to be chosen for Madagascar numbers (+261) |
| Files | Supabase Storage; the API issues signed upload and read URLs |
| Realtime | SignalR hub in the API (chat, booking status) |
| Background work | Hosted services: transactional outbox, expirations, push dispatch |
| Push | `expo-notifications` on device; the API calls the Expo push service |
| Payments | Local provider for Madagascar, to be decided (Phase 10) |
| Hosting | API as a Docker image on a European PaaS (Fly.io Paris or equivalent), next to the Supabase region |
| CI/CD | GitHub Actions: mobile checks, API checks, image build, staging deploy |

## Repository layout

```
apps/mobile/            Expo app
api/
  src/Lalana.Domain/          Entities, value objects, business rules (no I/O)
  src/Lalana.Application/     Use cases, ports (interfaces), DTOs
  src/Lalana.Infrastructure/  EF Core, Supabase, payment, push adapters
  src/Lalana.Api/             HTTP endpoints, auth, SignalR, hosted services
  tests/Lalana.Domain.Tests/
  tests/Lalana.Application.Tests/
  tests/Lalana.Api.IntegrationTests/   WebApplicationFactory + Testcontainers (PostGIS)
docs/
```

## Rules

1. **All business rules live in the API**, in `Domain` and `Application`. The mobile
   app never decides a price, a commission, a seat count, or a status transition.
2. **The mobile app never reads or writes app tables directly.** It talks to the API
   only, plus Supabase Auth for sign-in. The `app` schema is not exposed through
   Supabase's REST API (no grants to `anon` / `authenticated`).
3. **Generated API client.** The API publishes an OpenAPI document; the mobile app
   generates its TypeScript client and types from it. Screens call
   `src/services/<domain>.ts`, which wraps the generated client.
4. **Identity is the Supabase Auth user id.** The API validates the JWT (issuer,
   audience, signing keys from the project's JWKS) and loads the matching profile.
5. **Money is a `long` number of Ariary** (DB `bigint`), never a floating type. Every
   movement is an append-only ledger row; every provider call carries an idempotency
   key; every webhook is verified and processed idempotently.
6. **Side effects go through a transactional outbox.** A booking change and its
   notification events are written in the same transaction; a hosted service sends
   push notifications and SignalR events afterwards.
7. **Time is `timestamptz` (UTC)**, displayed in `Indian/Antananarivo`.
8. **Secrets stay server-side.** The mobile app holds only public configuration
   (API URL, Supabase URL and anon key). Provider keys and the database connection
   string live in the API's secret store.
9. **Migrations are EF Core migrations**, applied by the CI deploy step, never by hand
   on staging or production.

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
- Final product name, Supabase region, and API host (latency check from Antananarivo).
