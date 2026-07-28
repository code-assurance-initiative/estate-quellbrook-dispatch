# 3. Consume through an inbox, publish through an outbox

- Status: Accepted
- Date: 2026-07-28
- Deciders: Kasper Nyholt, Wendy Achterberg, Ruth Calloway (Orders)

## Context

RabbitMQ delivers at least once: a consumer that crashes after changing the database but before acknowledging gets
the message again. And a service that changes its database and then publishes can lose the event in between. Dispatch
does both.

## Decision

Every consumed message is processed in one database transaction that also records its AMQP `message-id` in an
`inbox_messages` table; a message id already recorded is acknowledged and skipped. Handlers are in addition written
to be idempotent (one consignment per order). Every published event is written to an `outbox_messages` table in the
same transaction as the change that raised it and published by a relay in the same process, with the outbox id as
message id. Messages that cannot be processed are rejected to the `quellbrook.dead-letter` exchange.

## Consequences

- Positive: no lost and no doubly applied messages; publishing survives broker outages.
- Negative: two more tables to keep small (retention), a short publishing delay, and dead-lettered messages need an
  owner (the Fleet on-call).
