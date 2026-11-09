# Phase 08 — Notifications

## Status

Not started

## Objective

Notify the right person at each booking step, by push and in an in-app list.

## Conversation context and prior decisions

- Required: a request notifies the driver; acceptance or refusal notifies the
  passenger; the passenger's confirmation notifies the driver. Driver offers on ride
  requests notify the passenger.

## Scope

- Push token registration (`POST /me/push-tokens`), removal on sign-out.
- Outbox processor: turns booking events into `Notification` rows and Expo push
  messages, with retries and receipt checks; invalid tokens are removed.
- API: `GET /me/notifications` (paginated), `POST /me/notifications/read`.
- Screen from the board: Notifications ("À traiter" requests with Accepter / Refuser,
  "Récentes"), bell badge on Accueil, deep links to the right screen.
- French copy for every notification type.

## Out of scope

- E-mail or SMS notifications (backlog).
- Notification preferences beyond on/off (backlog).

## Dependencies

- Phase 07 (outbox events).

## Implementation tasks

- [ ] Add push token endpoints and device registration.
- [ ] Add the outbox processor and Expo push adapter with retries.
- [ ] Add the notification list endpoints and screen.
- [ ] Add deep links from push and list items.

## Acceptance criteria

- [ ] Each booking transition produces exactly one notification for the right user.
- [ ] Tapping a push opens the related booking or request.
- [ ] A failed push send is retried and does not block the booking transaction.

## Test strategy

Unit tests for event-to-notification mapping. Integration tests for the outbox
processor with a fake push adapter. Component tests for the list and badges.

## Risks and security considerations

- Push content shows no payment or personal details beyond first names and places.
