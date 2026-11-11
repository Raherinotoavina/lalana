# Phase 04 — Profiles and vehicles

## Status

Not started

## Objective

Give every member a public profile (Facebook-like UX, own UI) and let drivers
register vehicles with photos, reused on every trip they publish.

## Conversation context and prior decisions

- Profile layout from the board: cover, large rounded-square avatar, name with
  verified badge, short bio, "Message" (primary action) and "Noter" buttons, stats (rating,
  trips offered, trips taken), vehicle, member since, reviews section.
- A vehicle has a type (voiture, moto, scooter), brand and model, colour, photos.
- Vehicles are entered the first time a member publishes an offer, then remembered.

## Scope

- API: `GET/PUT /me/profile`, `GET /profiles/{id}`, `GET/POST/PUT/DELETE /me/vehicles`.
- Photo upload: the app compresses the image on the device and uploads it to the API,
  which checks type and size, resizes it, and stores it through `IFileStorage` (disk
  volume of the host first). Photos are read through short-lived signed URLs issued
  by the API.
- Profile screen (own and others), edit-profile screen, vehicle form reused by
  Phase 05's publishing step 2.
- Placeholders for stats and reviews until Phases 07 and 11 provide data.

## Out of scope

- Reviews content and rating aggregates (Phase 11).
- Identity document verification (backlog).

## Dependencies

- Phase 03.

## Implementation tasks

- [ ] Add `Profile`, `Vehicle`, `VehiclePhoto` entities, migration, and endpoints.
- [ ] Add signed upload URLs and photo confirmation with size and type checks.
- [ ] Build the profile screens from the board.
- [ ] Build the vehicle form (type, brand and model, colour, photos).

## Acceptance criteria

- [ ] A member edits name, bio, and photo; others see the public profile.
- [ ] A member adds a vehicle with up to 4 compressed photos.
- [ ] Only the owner can change or delete a vehicle.

## Test strategy

Domain tests for vehicle validation. Integration tests for ownership rules and
upload confirmation. Component tests for the profile and vehicle forms.

## Risks and security considerations

- Validate file type and size server-side; strip photo metadata (location) on device.
- Public profile exposes no e-mail.
