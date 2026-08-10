# 4. Same-day express service

- Status: Accepted
- Date: 2026-08-10
- Deciders: Kasper Nyholt, Dario Fenwick, Ruth Calloway (Orders)

## Context

Orders accepts express orders since 0.1.0, but dispatch plans them like standard ones, for the next day. Sales has
promised same-day delivery in the Aarhus and Copenhagen zones from 2026-08-24.

## Decision

An express consignment received on a working day before the 14:00 cut-off (depot time) is assigned to a route of the
same day: preferably a dedicated express run, otherwise a standard route that has not left yet and still has a
10 % capacity buffer. A route of an adjacent zone may take a small express consignment (at most two parcels).
Consignments over 20 kg need a rigid vehicle and a C1 driver. A driver who has worked 4.5 hours without a started
route is not given more express stops (the mandatory break). After the cut-off an express consignment is planned as
standard for the next day and the shipper is credited.

## Consequences

- Positive: same-day delivery without a separate fleet; express runs fill up first.
- Negative: the assignment rules for express are more involved than for standard and depend on the time of day,
  which makes them harder to test; dispatchers must plan express runs before 10:00 to make use of them.
