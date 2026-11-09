# Phase 11 — Ratings and recommendations

## Status

Not started

## Objective

Let members rate each other after a completed trip and show the reputation on
profiles and trip cards.

## Conversation context and prior decisions

- Required: an account can rate another account, as a recommendation system shown on
  the profile.
- Board: rating sheet (5 stars, quality tags, optional comment visible on the
  profile), profile section "Ce qu'on dit de …" with tag counts and recent reviews.

## Scope

- `Review`: one per member per completed booking and direction (driver → passenger,
  passenger → driver), rating 1–5, tags, optional comment.
- API: `POST /bookings/{id}/review`, `GET /profiles/{id}/reviews` (paginated).
- Aggregates on the profile (average, count, tag counts), updated in the same
  transaction as the review.
- "Noter" prompt after a trip is completed; rating shown on trip cards and requests.
- Report an abusive review.

## Out of scope

- Moderation tooling beyond reports (backlog).

## Dependencies

- Phase 07 (completed bookings), Phase 04 (profiles).

## Implementation tasks

- [ ] Add `Review`, aggregates, and endpoints with rules (only participants, once).
- [ ] Build the rating sheet and the profile reviews section.
- [ ] Show ratings on cards and requests.

## Acceptance criteria

- [ ] Only the two participants of a completed booking can review each other, once.
- [ ] The profile average and tag counts update immediately.

## Test strategy

Domain tests for eligibility and aggregates. Integration tests for duplicates and
authorization. Component tests for the rating sheet.

## Risks and security considerations

- Retaliation reviews: consider revealing both reviews only after both are submitted
  or a delay passes (to decide).
