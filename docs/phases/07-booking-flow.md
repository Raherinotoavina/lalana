# Phase 07 — Booking flow

## Status

Not started

## Objective

Implement the two-step booking on segments: the passenger requests seats between two
stops, the driver accepts or refuses, the passenger confirms; and the mirror flow
where a driver offers a seat on a ride request.

## Conversation context and prior decisions

- Original requirement: a passenger can book one or more seats between any two stops,
  not necessarily the whole trip. Seat counts on posts reflect each segment.
- The owner confirms or refuses; then the traveller reconfirms; only then is the
  booking truly confirmed.
- A commission is taken from the driver at acceptance and from the passenger at
  confirmation (amounts and provider in Phase 10; this phase records the intent).
- Driver offers on ride requests follow the same machine (2026-11-09).

## Scope

- `Booking` and `Offer` entities with the state machine of the architecture.
- API: `POST /trips/{id}/bookings` (from stop, to stop, seats, message),
  `POST /bookings/{id}/accept`, `/refuse`, `/confirm`, `/cancel`,
  `GET /me/bookings` (as passenger), `GET /me/trips/bookings` (as driver),
  `POST /ride-requests/{id}/offers`, `POST /offers/{id}/accept`.
- Seat capacity check per segment inside one transaction with a trip row lock.
- Expiration by a hosted service: requests not answered, and accepted bookings not
  confirmed before the timeout (value in Open decisions).
- Outbox events for every transition (consumed by Phases 08 and 09).
- Screens from the board: Réserver (choose stops and seats, live total), Mes
  réservations (Mes places / Mes trajets), driver requests list with Accepter /
  Refuser, Proposer ma place.
- Confirmation is a placeholder step until Phase 10 adds payment.

## Out of scope

- Payment and commission collection (Phase 10).
- Cancellation penalties (to be decided).

## Dependencies

- Phase 06.

## Implementation tasks

- [ ] Model the state machine in `Domain` with exhaustive transition tests.
- [ ] Implement capacity checks with locking, and a concurrent integration test.
- [ ] Add the endpoints, authorization (only the driver accepts, only the passenger
  confirms), and outbox writes.
- [ ] Add the expiration hosted service.
- [ ] Build the booking, my-bookings, driver requests, and offer screens.

## Acceptance criteria

- [ ] Two simultaneous acceptances for the last seat on a segment: exactly one wins.
- [ ] A passenger booking Andraharo → Analakely leaves Ivato → Andraharo seats free.
- [ ] Invalid transitions (confirm before accept, accept by the passenger) are
  rejected.
- [ ] Unanswered requests expire automatically.

## Test strategy

Domain tests for every transition and capacity case. Integration tests with real
Postgres for locking and concurrency. Component tests for the booking screen totals.

## Risks and security considerations

- Race conditions are the main risk: never check capacity outside the locked
  transaction.
