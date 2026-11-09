# Phase 03 — Authentication

## Status

Not started

## Objective

Let people create an account and sign in with a mobile number or an e-mail and a
password, or with Google; confirm the account with a code; reset the password; then
create their profile in the API on first use.

## Conversation context and prior decisions

- Sign-in has one identifier field, "Téléphone ou email", plus a password, like
  Facebook (decided 2026-11-09, replacing the earlier "Téléphone | Email" switch).
  The app detects which one was typed.
- Sign-up has two separate fields, "Numéro de mobile" (+261) and "Email", plus a
  password (decided 2026-11-09). At least one of the two is required; the other is
  optional. A phone is confirmed by a 6-digit SMS code, an e-mail by a 6-digit e-mail
  code. When both are given, the SMS code is asked during sign-up and the e-mail is
  confirmed afterwards, without blocking access.
- Google sign-in on both screens ("Continuer avec Google", "S'inscrire avec Google").
- No driver/passenger role at sign-up: every account can do both. Driver details
  (vehicle, payment method) are asked only when first needed.
- Sign-up asks first name, last name, mobile number and/or e-mail, and password
  (8 characters, including a digit).
- Board review of the auth screens (2026-11-09). The board now shows:
  - Password reset in three screens: identifier → code (Mot de passe oublié — code)
    → new password (Nouveau mot de passe). The first screen never says whether an
    account exists ("Si un compte existe, vous recevrez un code"). Saving the new
    password signs the user in (Supabase recovery session) and opens Accueil.
  - Code entry is one numeric field with `autocomplete="one-time-code"`, drawn as
    six boxes, so iOS and Android can offer the received SMS code. It submits on its
    own when the sixth digit is typed; the "Vérifier" button stays for clarity.
  - Error and loading states on every auth screen (the "etat" tweak on each
    artboard): wrong credentials, invalid phone format, unverified account, too many
    attempts, identifier already used, no identifier, weak password, wrong or expired
    code.
  - A user whose account is not verified who signs in is sent to Vérification.
  - When the resend timer ends, "Renvoyer le SMS" and, if the account also has an
    e-mail, "Recevoir le code par email" (fallback for SMS delivery problems). The
    phone then stays to confirm later.
  - An e-mail still to confirm shows a reminder card on Accueil ("emailAConfirmer"
    tweak), which opens Vérification.
  - The password rules are a live checklist under the field, not a placeholder.
  - A step bar on sign-up (2 steps) and reset (3 steps); Google on Connexion shows
    the terms notice.
  - In the app, primary actions are real buttons (the board uses links only to move
    between artboards), and the show-password button switches its icon and label
    ("Afficher" / "Masquer le mot de passe").

## Scope

- Screens from the board: Connexion (one identifier field + password), Inscription
  (separate phone and e-mail fields, at least one, + password), Vérification (SMS or
  e-mail variant), Mot de passe oublié (three screens: phone or e-mail, code, new
  password), and the e-mail reminder card on Accueil, each with its error and loading
  states.
- Identifier parsing: a value containing `@` is an e-mail; otherwise it is a phone
  number. Local formats (`034 12 345 67`, `34 12 345 67`) and `+261…` are normalized
  to E.164 and Malagasy mobile prefixes are validated; anything else shows a clear
  error.
- Supabase Auth: sign-up with the phone (SMS code, chosen SMS provider) or the
  e-mail (e-mail code) and the password; when both are given, the e-mail is attached
  to the same user and confirmed by its own code;
  sign-in with either identifier and the password; reset by SMS or e-mail code,
  Google provider (native sign-in on Android and iOS).
- Session persistence and refresh on the device; sign-out.
- API: `POST /profiles/bootstrap` (idempotent) creating the profile from the token's
  identity (phone, e-mail, or Google) and the submitted names; `GET /me` returns it.
- Adding the missing identifier later, or changing one, from the profile settings,
  with verification.
- Auth guard in Expo Router: signed-out users see only the auth screens.
- French copy and clear errors (wrong or expired code, number or e-mail already used,
  too many attempts).

## Out of scope

- Account deletion (Phase 12).
- Identity document verification (backlog).

## Dependencies

- Phases 01 and 02. A Google Cloud OAuth client (Android, iOS, web). An SMS provider
  account configured in Supabase Auth.

## Implementation tasks

- [ ] Compare SMS providers supported by Supabase Auth for +261 numbers (delivery,
  price, fraud protection) and record the choice here.
- [ ] Configure Supabase Auth: phone OTP, e-mail templates in French, code length,
  Google provider, rate limits.
- [ ] Build the auth screens from the board (single field at sign-in, two at sign-up).
- [ ] Implement identifier parsing and phone normalization with unit tests.
- [ ] Implement the auth service (the only place importing Supabase).
- [ ] Add native Google sign-in and exchange its token with Supabase.
- [ ] Add the profile bootstrap endpoint and its tests.
- [ ] Add or change the phone or e-mail in the account settings.
- [ ] Add the route guard and session handling.

## Acceptance criteria

- [ ] Sign-up works with only a mobile number (SMS code), with only an e-mail
  (e-mail code), and with both; it is refused when both are empty.
- [ ] A user with both signs in with `034 …` and the password, and with the e-mail and
  the password.
- [ ] A number or e-mail already used shows a clear error at sign-up.
- [ ] Password reset works from a phone number (SMS code) and from an e-mail.
- [ ] Google sign-in works on an Android build and creates the profile once.
- [ ] Calling bootstrap twice does not create two profiles.
- [ ] Repeated code requests for the same number are rate-limited.
- [ ] The SMS code is offered by the keyboard (iOS) or SMS autofill (Android) and is
  submitted on its own at the sixth digit.
- [ ] The reset screen shows the same message whether or not the account exists.
- [ ] Every error and loading state drawn on the board is reachable and matches it.

## Test strategy

Component tests for each screen with a phone and an e-mail identifier (validation,
errors, loading). Unit tests for identifier parsing and phone normalization. Service tests with a fake Supabase client. API
integration tests for bootstrap idempotency and `GET /me` for each identity type.

## Risks and security considerations

- SMS cost and SMS-pumping fraud: restrict to +261, rate-limit per number and per
  device, and monitor the volume sent.
- Delivery may vary by operator (Telma/Yas, Orange, Airtel): test all three.
- Never log tokens, codes, or full phone numbers.
- E-mail deliverability: use a verified sending domain before beta.

## Notes and decisions

Check the current Expo and Supabase docs for phone OTP and native Google sign-in
before choosing libraries.
