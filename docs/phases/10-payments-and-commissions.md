# Phase 10 — Payments and commissions

## Status

Not started — research can start now; implementation is blocked on the provider
decision.

## Objective

Collect the platform commission from the driver at acceptance and from the passenger
at confirmation, reliably and without double charges.

## Conversation context and prior decisions

- Original requirement: a payment method is required from the driver when confirming
  a booking, and a Visa card from the traveller when reconfirming; a commission is
  taken from each.
- Stripe is believed not to support merchants based in Madagascar (to verify); a local
  provider is needed. Mobile Money (MVola, Orange Money, Airtel Money) is the dominant
  payment habit; card ownership is low.
- Recommendation under discussion: an in-app balance topped up by Mobile Money or card,
  with commissions debited from it, because per-transaction card fees would exceed
  small commissions.

## Scope

- Research (first task, can start now): providers available to a Madagascar business,
  supported methods (Mobile Money, Visa), fees, settlement, webhook model, sandbox,
  legal requirements; write the comparison in this file and get the owner's choice.
- `LedgerEntry` (append-only) and balance projection per member.
- Payment adapter behind an `IPaymentProvider` port: start payment, verify webhook,
  query status, refund.
- Commission rules in `Domain` (amounts configurable, Ariary integers).
- Hooks into Phase 07: acceptance requires the driver's commission to succeed;
  confirmation requires the passenger's payment to succeed; failures keep the
  booking in its previous state.
- Screens from the board: Confirmer ma place (summary, fare, service fee, card form or
  Mobile Money), payment method management, balance and history.

## Out of scope

- Paying the fare itself through the app, if the owner keeps cash fares.
- Payouts to drivers (only if fares go through the app).

## Dependencies

- Phase 07. The provider decision.

## Implementation tasks

- [ ] Research providers and record the comparison and decision.
- [ ] Add the ledger, balances, and commission rules with unit tests.
- [ ] Implement the provider adapter, idempotency keys, and webhook verification.
- [ ] Connect acceptance and confirmation to payment results.
- [ ] Build the payment screens.

## Acceptance criteria

- [ ] A replayed webhook does not credit or debit twice.
- [ ] A failed payment leaves the booking unchanged and tells the user why.
- [ ] The ledger always balances (sum of entries equals the projected balances).
- [ ] Card data never reaches the API (the provider's hosted fields or redirect).

## Test strategy

Domain tests for commission rules. Integration tests with a fake provider for
success, failure, timeout, and replayed webhooks. A manual end-to-end test in the
provider's sandbox.

## Risks and security considerations

- Never store card numbers; rely on the provider's PCI-compliant collection.
- Regulatory constraints on holding user balances must be checked with the provider.
