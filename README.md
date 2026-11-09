# Lalana

Carpooling app for Madagascar: drivers offer their free seats, passengers find and book
them, per segment of the route.

| Path | What |
| --- | --- |
| `apps/mobile/` | Expo / React Native app (TypeScript, Expo Router), Android first |
| `api/` | ASP.NET Core API that owns every business rule (Phase 02) |
| `docs/` | [Architecture](docs/architecture.md), [phases](docs/phases/README.md), [backlog](docs/features/README.md) |

The backend is self-hosted: the API, PostgreSQL + PostGIS, accounts (ASP.NET Core
Identity) and photo storage run in Docker, locally for now and on one small VPS later.
See [architecture](docs/architecture.md).

Design board: Claude Design canvas "Covoiturage — Maquette V1"
(https://claude.ai/artifact/AeiqLeQhtQeiZi2e6Pevty).

## Requirements

- Node.js 24 and npm
- For a device build: Android Studio (emulator) or a phone with a development build
- For the API (from Phase 02): .NET SDK 10 and Docker

## Getting started

```bash
npm install                 # repository tooling and git hooks
cd apps/mobile
npm install
cp .env.example .env.local  # then fill in the values
npx expo start
```

The app uses native modules beyond Expo Go's set over time; use a development build
(`npx expo run:android`) when Expo Go is not enough.

## Mobile scripts (`apps/mobile`)

| Command | Does |
| --- | --- |
| `npm run lint` | ESLint (Expo config + Prettier), no warnings allowed |
| `npm run typecheck` | TypeScript, strict |
| `npm run format` / `format:check` | Prettier |
| `npm test` / `npm run test:coverage` | Jest; coverage must stay ≥ 80 % (statements, branches, functions, lines) |
| `npm run validate` | All of the above, as CI runs it |

Add packages with `npx expo install <package>` so versions match the Expo SDK.

## Environment variables

`apps/mobile/.env.example` lists them. They are `EXPO_PUBLIC_` variables, embedded in
the app and readable by anyone: never put a secret there. Real values go in
`.env.local`, which git ignores.

| Variable | Purpose |
| --- | --- |
| `EXPO_PUBLIC_API_URL` | Lalana API base URL (`http://10.0.2.2:5080` from the Android emulator) |

## Git workflow and checks

- Never push to `main`. Branch from an up-to-date `main` for each feature or fix and
  open a pull request; CI must pass before merging.
- `pre-commit` (Husky + lint-staged): ESLint, related Jest tests and typecheck on the
  staged files.
- `pre-push`: the complete suite (`npm run validate` at the root), with coverage.
- CI (`.github/workflows/ci.yml`) runs the same checks on every pull request and on
  `main`: lint, typecheck, format, tests, and a coverage gate that fails below 80 % on
  any metric and writes the coverage table to the job summary. The `CI OK` job passes
  only when every check passed; it is the check to require before merging. Hooks can be
  skipped locally; CI cannot.

## Code layout (`apps/mobile/src`)

| Folder | Holds |
| --- | --- |
| `app/` | Routes only (Expo Router); each file is a screen, `_layout.tsx` defines navigators |
| `features/<domain>/` | Screens' content and TanStack Query hooks for one domain |
| `services/` | API access (generated client wrappers), the query client, configuration |
| `domain/` | Shared types |
| `ui/` | Design-system components and the icon set (Lucide; never emoji as icons) |
| `theme/` | Tokens from the board: colours, type, spacing, radii |

Only `src/services/` may import the generated API client; screens and features use
TanStack Query hooks. Lint fails otherwise.
