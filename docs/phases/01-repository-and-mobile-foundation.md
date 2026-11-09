# Phase 01 — Repository and mobile foundation

## Status

In progress — code and checks on branch `feat/01-repository-and-mobile-foundation`
(pull request open). Remaining: run the app on an Android development build and
compare the tab bar with the board; first green CI run.

## Objective

Create the monorepo, the Expo app shell with the validated design tokens, and the
quality gates (tests, coverage, hooks, CI) that every later phase relies on.

## Conversation context and prior decisions

- Stack agreed on 2026-11-09: Expo mobile app + C# / ASP.NET Core API from V1 +
  Supabase (Postgres, Auth, Storage). See [architecture](../architecture.md).
- Palette "Nuit + Soleil": primary `#172554`, accent `#FFD23F`, ink `#111827`,
  background `#F5F4F0`, muted `#5A6070`. Font Plus Jakarta Sans. Avatars are rounded
  squares; the tab bar is a dark floating bar with a yellow "+" in the centre.
- UX is inspired by Facebook; the UI must not look like Facebook. Simplicity first:
  one yellow call to action per screen.
- Android first; low-end devices and 3G/4G networks.
- Server state on the mobile app goes through TanStack Query over the generated API
  client (decided 2026-11-09); see architecture rule 3. The query client is set up in
  this phase; the generated client arrives in Phase 02.
- Coverage is at least 80 % per project (mobile and API separately), never averaged
  (decided 2026-11-09).

## Scope

- Monorepo layout (`apps/mobile`, `api`, `docs`), root README, `.gitignore`,
  `.editorconfig`.
- Expo app on the latest SDK (read the versioned docs first), TypeScript strict, Expo
  Router routes in `apps/mobile/src/app/`.
- Mobile folders: `src/app` (routes), `src/features/<domain>`, `src/services` (API
  access), `src/domain` (types), `src/ui` (design-system components), `src/theme`.
- Theme tokens and base components from the board: accent and ghost buttons, inputs,
  cards, chips, rounded-square avatar, segmented control, floating tab bar, icon set
  (project icon library, never emoji as icons).
- Tab bar shell: Accueil, Réservations, Publier, Messages, Profil.
- ESLint rule forbidding `@supabase/*` imports outside `src/services/auth*`.
- Jest (`jest-expo`) + React Native Testing Library, 80 % global thresholds.
- Husky `pre-commit` (lint-staged on staged mobile and C# files) and `pre-push`
  (complete validation of both apps).
- GitHub Actions workflow with a mobile job (lint, typecheck, tests with coverage).
- `.env.example` listing the public variables (API URL, Supabase URL, anon key).

## Out of scope

- The API (Phase 02) and any real data.
- EAS release builds (Phase 12).

## Dependencies

- A GitHub repository.

## Implementation tasks

- [x] Initialize the repository and the monorepo layout on a feature branch
  (GitHub `Raherinotoavina/lalana`, private).
- [x] Create the Expo app (SDK 58, React Native 0.88, React 19.3) and its folder structure.
- [x] Add theme tokens and base UI components with tests (Button accent/ghost,
  TextField, Card, Chip, Avatar, SegmentedControl, Logo, Screen, icon set from Lucide).
- [x] Build the tab bar shell matching the board (`expo-router/ui` custom tabs).
- [x] Add the import-restriction lint rule (tested by running ESLint).
- [x] Configure Jest, coverage thresholds, and validation scripts.
- [x] Set up TanStack Query (query client, retry policy, app-focus refetch).
- [x] Add Husky hooks and the mobile CI job. The C# pre-commit checks come with
  Phase 02 (`api/.lintstagedrc`), and `scripts/validate.sh` already runs the API suite
  once `api/` exists.
- [x] Write the README (setup, scripts, environment variables).

## Acceptance criteria

- [ ] The app runs on an Android development build and shows the tab bar shell
  matching the board.
- [ ] Lint, typecheck, and Jest with coverage ≥ 80 % pass locally and in CI. Local:
  passing (100 % statements, functions and lines, 91 % branches). CI: pending first run.
- [x] Importing `@supabase/supabase-js` outside the auth service fails lint.
- [x] The pre-push hook runs the complete validation suite.

## Test strategy

Component tests for every base UI component (variants, disabled state, accessibility
labels) and for the tab bar navigation.

## Risks and security considerations

- Mobile environment variables are public; never put secrets in them.
- Hooks can be bypassed; CI is the authoritative check.

## Notes and decisions

The lint rule is the guarantee that the mobile app reaches data only through the API.
