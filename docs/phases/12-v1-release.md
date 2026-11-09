# Phase 12 — V1 stabilization and release

## Status

Not started

## Objective

Harden, measure, and ship V1 to a closed beta, then to the Play Store (and App Store
if iOS is in scope).

## Conversation context and prior decisions

- Android first. Low-end devices and unstable 3G/4G connections.
- Launch on one or two busy corridors (for example Ivato ⇄ city centre) to solve the
  chicken-and-egg problem, rather than the whole city.

## Scope

- Production environment: Supabase project, API deployment, secrets, backups,
  migration procedure, rollback plan.
- Monitoring: API error tracking, structured logs, uptime checks, mobile crash
  reporting.
- Performance pass on a low-end Android device; payload and image size budget.
- Account deletion (app and API) and data export on request.
- Legal pages in French: terms, privacy policy, cost-sharing rules, safety advice.
- EAS builds and EAS Update channels; store listing, screenshots, privacy forms.
- Closed beta with real drivers and passengers on the chosen corridors; feedback loop.
- Final regression of every phase's acceptance criteria.

## Out of scope

- Features from the backlog.

## Dependencies

- Phases 01–11.

## Implementation tasks

- [ ] Create and document the production environment.
- [ ] Add monitoring and alerting.
- [ ] Run the low-end device performance pass.
- [ ] Implement account deletion.
- [ ] Publish legal pages.
- [ ] Prepare store assets and builds.
- [ ] Run the closed beta and fix blocking issues.

## Acceptance criteria

- [ ] Production deploys from CI only, with a tested rollback.
- [ ] A deleted account's personal data is removed or anonymized.
- [ ] The beta runs one week on the chosen corridors without a blocking issue.

## Test strategy

Full regression on Android builds, plus the automated suites of both apps.

## Risks and security considerations

- Legal status of paid carpooling in Madagascar must be checked before public launch.
