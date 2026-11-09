This is a monorepo for a carpooling product: an Expo/React Native mobile application in `apps/mobile/` and a C# / ASP.NET Core API in `api/`. Prioritize mobile-first patterns, performance, and cross-platform compatibility. Read `docs/architecture.md` before any change; its rules are mandatory.

The Expo sections below apply inside `apps/mobile/` (paths such as `src/app/` are relative to it).

## Backend (`api/`)

- Target the current .NET LTS. Before using an ASP.NET Core, EF Core, Npgsql, or SignalR API, check the official Microsoft Learn docs for that version; do not rely on memory.
- All business rules (prices, commissions, seat capacity per segment, booking status transitions) live in `Lalana.Domain` and `Lalana.Application`. Endpoints stay thin. The mobile app never decides them.
- The mobile app talks to the API only (generated OpenAPI client, used through TanStack Query hooks). Accounts and sign-in are ASP.NET Core Identity in the API; there is no backend-as-a-service.
- Everything runs locally with Docker (PostgreSQL + PostGIS, the API). Keep the stack runnable on one small VPS with Docker Compose; choose the lowest-cost option that stays always-on (see `docs/architecture.md`).
- Money is `long` Ariary, never `float`, `double`, or `decimal` rounding tricks. Ledger rows are append-only. Provider calls and webhooks are idempotent.
- Schema changes are EF Core migrations, applied by CI, never by hand on shared environments.
- Commands:

```bash
dotnet build api --warnaserror      # build, warnings are errors
dotnet format api --verify-no-changes
dotnet test api                     # unit + integration tests (Testcontainers needs Docker)
```

- Keep API coverage at or above 80% (lines and branches), enforced in the test configuration and CI, like the mobile app.

## Expo has changed — do not trust your training data

Expo ships breaking changes every SDK release. APIs you remember are likely renamed, moved, or removed. Before writing any code that touches an Expo, EAS, or React Native API:

1. Read the major version of the `expo` package in `package.json`.
2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`
3. For anything else, fetch https://docs.expo.dev/llms.txt — an index of all Expo docs with corrections to common LLM misconceptions. Follow its links to the specific page you need; never answer from memory.

## Commands

Use `bunx` instead of `npx` if the project uses bun (`bun.lock` present).

```bash
npx expo install <package>  # ALWAYS use instead of npm/yarn/pnpm/bun add — resolves SDK-compatible versions
npx expo start              # start the dev server
npx expo lint               # lint
npx tsc --noEmit            # typecheck
npx expo-doctor             # diagnose dependency and config issues
npx expo install --fix      # fix incompatible package versions
```

Run lint and typecheck before declaring any task done.

## Navigation & Routing

- Use **Expo Router** for all navigation. Routes live in `src/app/` — every file there is a screen, `_layout.tsx` files define navigators. Keep non-route code (components, hooks, utils) outside `src/app/`.
- Import `Link`, `router`, and `useLocalSearchParams` from `expo-router`.
- Docs: https://docs.expo.dev/router/introduction.md

## Building with EAS

Use EAS to build, sign, and submit the app in the cloud (`eas build`, `eas submit`) and to ship over-the-air updates (`eas update`) — no local Xcode or Android Studio required. Run EAS CLI as `bunx eas-cli <command>` in Bun projects, or `npx eas-cli@latest <command>` otherwise; substitute that for bare `eas` in docs examples.
Docs: https://docs.expo.dev/eas/index.md

## Rules

- If `ios/` and `android/` directories do not exist, they are generated (Continuous Native Generation). Never create or edit them by hand — configure native behavior in `app.json` and config plugins.
- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a development build: `npx expo run:ios|android` locally, or `eas build --profile development`.
- Prefer recommended Expo modules over third-party libraries, and check your available skills before adding dependencies. Docs: https://docs.expo.dev/versions/latest/index.md

## Development workflow

- Never push directly to `main`. Integrate changes through a pull request after the required checks pass.
- Create a new dedicated branch from an up-to-date `main` for every feature or fix. Do not reuse a branch for unrelated changes.
- Add and maintain a GitHub Actions workflow that runs the project's required quality checks on pull requests and pushes to protected branches.
- Every production code change must include appropriate automated tests. Keep the global code coverage at or above 80% for statements, branches, functions, and lines, and enforce these thresholds in both the test configuration and CI.
- Add and maintain repository-managed `pre-commit` and `pre-push` hooks. The pre-commit hook must run fast checks relevant to staged changes; the pre-push hook must run the complete required validation suite, including tests and coverage.
- Never use emoji as interface icons or as a substitute for visual controls. Use the project's icon library instead. If no icon library exists yet, prefer an Expo-compatible library and install it with `npx expo install`.
- Never hard-code secrets, credentials, tokens, private endpoints, or other security-sensitive configuration. Store them in environment variables, document required variable names in a safe example file, and never commit real values. Remember that variables embedded in a mobile client are not secret; truly confidential values must remain on a trusted backend.
- Run lint, typecheck, and the relevant tests before declaring a code task complete.

## Product implementation phases

- Before implementing a feature, analyze the relevant prior conversation and existing repository context so the work remains consistent with decisions already made.
- Before building or changing any screen, widget, or other visual surface, analyze its design board (the Claude Design canvas "Covoiturage — Maquette V1" (https://claude.ai/artifact/AeiqLeQhtQeiZi2e6Pevty), or a screenshot the owner shares) and follow its layout, hierarchy, labels, icons, and states. A text description in a phase file is not a substitute for the board. If the board cannot be opened, ask the owner for it before implementing, and compare the result with the board before declaring the work done.
- Store implementation plans in `docs/phases/`. Each phase must have its own Markdown file and describe its objective, scope, dependencies, tasks, acceptance criteria, test strategy, and status.
- Use sequential filenames such as `01-foundation.md`, `02-authentication.md`, and `03-core-experience.md`.
- Update the relevant phase file as implementation progresses. Do not mark a phase complete until its acceptance criteria pass and its required tests meet the coverage policy.
- Store ideas planned after V1 in `docs/features/`. These files form a product backlog, not an implementation commitment. Promote a feature into a numbered phase only after its prerequisites, product value, and scope have been validated.
