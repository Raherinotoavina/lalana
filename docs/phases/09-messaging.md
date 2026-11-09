# Phase 09 — Messaging

## Status

Not started

## Objective

Let any two members chat in real time, with the related trip pinned in the
conversation.

## Conversation context and prior decisions

- Required: a chat system so all accounts can message each other.
- Board: Messages list (unread count, trip context), Conversation with the trip card
  pinned on top ("À confirmer par vous" + Confirmer), share position button.

## Scope

- `Conversation` (two participants, optional trip or request link) and `Message`.
- API: `GET /me/conversations`, `POST /conversations` (find or create),
  `GET /conversations/{id}/messages` (cursor), `POST /conversations/{id}/messages`,
  `POST /conversations/{id}/read`.
- SignalR hub authenticated with the Supabase JWT: new messages and read receipts;
  the app falls back to polling when the connection drops.
- New-message push through Phase 08's outbox (not when the conversation is open).
- Block and report a member (minimum safety).
- Screens from the board: Messages and Conversation.

## Out of scope

- Photos in chat, voice messages, group chats (backlog).

## Dependencies

- Phase 04 (profiles); Phase 08 for push.

## Implementation tasks

- [ ] Add entities, endpoints, and authorization (participants only).
- [ ] Add the SignalR hub and the mobile realtime service with reconnection.
- [ ] Add block and report.
- [ ] Build the Messages and Conversation screens.

## Acceptance criteria

- [ ] A message appears on the other device within 2 seconds on a normal connection.
- [ ] A non-participant cannot read or post in a conversation.
- [ ] A blocked member cannot send messages to the blocker.

## Test strategy

Integration tests for endpoints and hub authorization. Component tests for message
list states. A manual two-device check.

## Risks and security considerations

- Abuse: keep reports reviewable; rate-limit message sending.
- Verify the SignalR JavaScript client works in React Native before committing to it.
