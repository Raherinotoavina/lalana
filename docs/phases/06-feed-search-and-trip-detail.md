# Phase 06 — Feed, search, and trip detail

## Status

Not started

## Objective

Let members browse upcoming trips and ride requests, search by route and time, and
see a trip's stops with the free seats of each segment.

## Conversation context and prior decisions

- Home: "Où allez-vous ?" search card, then a segmented control "Conducteurs |
  Passagers": offers on one side, ride requests on the other.
- Search is limited to route + time (departure, arrival, day, time). Vehicle, price,
  and rating filters were removed for simplicity (2026-11-09). Seats are chosen at
  booking time.
- A trip matches when it has a stop near the departure followed by a stop near the
  arrival, within the time window.

## Scope

- API: `GET /feed/trips` and `GET /feed/requests` (cursor pagination, upcoming only),
  `GET /trips/search` (PostGIS distance on stops + stop order + time window),
  `GET /trips/{id}` with per-segment free seats.
- Per-segment availability computed by the domain rule of the architecture (seats
  minus `Accepted` and `Confirmed` bookings per segment).
- Screens from the board: Accueil (both tabs), Recherche, Détail du trajet, recent
  searches stored on the device.
- Light payloads: small image sizes, paginated lists, list virtualization.

## Out of scope

- Shareable web trip pages (backlog).
- Saved searches and alerts (backlog).

## Dependencies

- Phase 05.

## Implementation tasks

- [ ] Add the availability calculation in `Domain` with unit tests.
- [ ] Add feed and search queries with spatial indexes and integration tests.
- [ ] Build the home tabs, search, and trip detail screens.

## Acceptance criteria

- [ ] Searching Ivato → Analakely at 07:00 finds a trip Ivato → Andraharo →
  Analakely leaving at 07:00, and a trip starting earlier that passes both places.
- [ ] Trip detail shows free seats for each segment, "Complet" when full.
- [ ] Lists stay smooth on a low-end Android device.

## Test strategy

Domain tests for availability with overlapping bookings. Integration tests for
search on seeded places and trips. Component tests for empty and error states.

## Risks and security considerations

- Search radius and time window values need tuning with real users.
