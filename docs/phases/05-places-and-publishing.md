# Phase 05 — Places and publishing

## Status

Not started

## Objective

Let a driver publish a trip offer with ordered stops, times, and per-segment prices,
and let a passenger publish a ride request ("Je cherche un trajet").

## Conversation context and prior decisions

- "Publier" first asks: "Je propose des places" or "Je cherche un trajet"
  (decided 2026-11-09).
- Offer in 3 steps: Trajet (date, departure, optional intermediate stops, arrival,
  each with a time), Véhicule (remembered), Places et prix (seats, price per seat for
  each segment, a short message; emojis allowed).
- Request in 1 step: departure, arrival, day, time with flexibility (± 15 min,
  ± 30 min, ± 1 h), seats, optional budget per seat.
- Addresses in Antananarivo are poorly geocoded: use a curated list of known places
  plus a pin on the map.

## Scope

- `Place` catalogue (neighbourhoods, landmarks) seeded for Antananarivo, with PostGIS
  points and a search-as-you-type endpoint.
- API: `POST /trips`, `GET /trips/{id}`, `PATCH /trips/{id}` (before any booking),
  `DELETE /trips/{id}` (cancel rules), `POST /ride-requests`, `DELETE /ride-requests/{id}`.
- Domain rules: at least 2 stops, strictly increasing times, departure in the future,
  seats 1–8 (car) or 1–2 (two-wheelers), prices as positive Ariary integers.
- Screens from the board: choice screen, 3-step offer wizard, request form, place
  picker.

## Out of scope

- Recurring trips (backlog).
- Free-form address geocoding (backlog).

## Dependencies

- Phase 04 (vehicles).

## Implementation tasks

- [ ] Add `Place`, `Trip`, `TripStop`, `RideRequest`, migrations, and seed data.
- [ ] Add the domain validation rules with unit tests.
- [ ] Add the endpoints and integration tests.
- [ ] Build the choice screen, offer wizard, request form, and place picker.

## Acceptance criteria

- [ ] A driver publishes Ivato → Andraharo → Analakely with times and two prices.
- [ ] A passenger publishes a request with a time window and budget.
- [ ] Invalid inputs (decreasing times, past date, too many seats) are rejected by the
  API with clear messages.

## Test strategy

Domain unit tests for every rule. Integration tests for creation and ownership.
Component tests for the wizard navigation (next, back, choice screen).

## Risks and security considerations

- The place catalogue quality decides search quality; plan a way to add places.
