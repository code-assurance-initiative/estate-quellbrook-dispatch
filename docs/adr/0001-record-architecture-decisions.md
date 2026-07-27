# 1. Record architecture decisions

- Status: Accepted
- Date: 2026-07-27
- Deciders: Kasper Nyholt, Wendy Achterberg

## Context

Dispatch is new, sits between the order service and the notifier, and will be changed by people who were not there
when it was designed. Decisions made in conversation are lost.

## Decision

We record every decision that shapes the service's structure, contracts or operation as an architecture decision
record in `docs/adr`, numbered, with status, context, decision and consequences.

## Consequences

- Positive: the reasons behind the structure survive staff changes; reviews can point at a record.
- Negative: records take time to write and must be marked superseded when a decision changes.
