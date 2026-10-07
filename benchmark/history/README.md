# Scripted history

33 commits by fictional authors, rebuilt and verified by `build-history.sh` (see its header). Commits after them are real benchmark maintenance.

## Sprint 1 (2026-07-27 – 2026-08-07, release `v0.1.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `c6cd9a5` | 2026-07-27 | Kasper Nyholt | chore: solution skeleton with central package management and lock files |  |
| `545daf1` | 2026-07-27 | Kasper Nyholt | docs: ADR 0001 (record decisions) and ADR 0002 (endpoints call application handlers) |  |
| `657cde8` | 2026-07-28 | Wendy Achterberg | feat(domain): consignments, delivery zones, routes, drivers and vehicles |  |
| `8623e31` | 2026-07-28 | Kasper Nyholt | docs: ADR 0003 inbox and outbox |  |
| `10176be` | 2026-07-29 | Wendy Achterberg | feat: persistence, inbox and outbox over PostgreSQL |  |
| `e9611c8` | 2026-07-30 | Kasper Nyholt | feat: consignments from orders.order-placed.v1 through the inbox |  |
| `682bde4` | 2026-07-31 | Wendy Achterberg | feat: standard assignment, starting routes and recording deliveries |  |
| `296e100` | 2026-08-03 | Kasper Nyholt | feat(api): fleet, routes and consignment endpoints with scoped bearer authentication |  |
| `e81b2c9` | 2026-08-04 | Wendy Achterberg | build: container image and Kubernetes manifests |  |
| `1118763` | 2026-08-05 | Kasper Nyholt | ci: build, test, CodeQL, release and deploy workflows |  |
| `ece56ad` | 2026-08-06 | Wendy Achterberg | docs: README, architecture with C4 diagrams, API contract and catalog entry |  |
| `b56df9a` | 2026-08-07 | Kasper Nyholt | release 0.1.0 | tag `v0.1.0` |

## Sprint 2 (2026-08-10 – 2026-08-21, release `v0.2.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `f03edd6` | 2026-08-10 | Kasper Nyholt | docs: ADR 0004 same-day express service |  |
| `25d7540` | 2026-08-11 | Dario Fenwick | feat(express): same-day express assignment | THE REGRESSION begins: one large method, the capacity/shift/licence block copied from the standard policy, two tests |
| `c47d56a` | 2026-08-12 | Wendy Achterberg | feat: drop the consignments of cancelled orders (orders.order-cancelled.v1) |  |
| `3753e72` | 2026-08-13 | Dario Fenwick | feat(api): drivers and vehicles available for the board | regression: the endpoint queries the database directly, against ADR 0002 |
| `357e7ff` | 2026-08-17 | Dario Fenwick | fix(express): respect the driver's break after 4.5 hours | regression: more branches in the express method |
| `011ae4a` | 2026-08-18 | Dario Fenwick | fix(express): heavy express consignments need a rigid vehicle and a C1 driver | regression: more branches in the express method |
| `c61d181` | 2026-08-19 | Kasper Nyholt | test: skip the route capacity test that fails around midnight | regression: a failing test is skipped instead of fixed |
| `8753f6f` | 2026-08-20 | Dario Fenwick | fix(express): adjacent zones may take small express consignments | regression: more branches in the express method |
| `a4b4187` | 2026-08-21 | Kasper Nyholt | docs: express, cancellations and the drivers endpoint |  |
| `a82838a` | 2026-08-21 | Kasper Nyholt | release 0.2.0 | tag `v0.2.0` |

## Sprint 3 (2026-08-24 – 2026-09-04, release `v0.3.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `0fc519a` | 2026-08-24 | Wendy Achterberg | test: route capacity test on a fixed date, enabled again | PARTIAL REPAIR: the skipped test is fixed and re-enabled |
| `71ac59c` | 2026-08-25 | Wendy Achterberg | refactor(express): cut-off, driver hours, zone and vehicle fit as their own rules | PARTIAL REPAIR: part of the express method extracted (still above the threshold; the copied block stays) |
| `0b83cf7` | 2026-08-26 | Kasper Nyholt | test(express): cut-off, driver hours, adjacent zones, heavy consignments and the capacity buffer | PARTIAL REPAIR: express scenarios tested |
| `9e05489` | 2026-08-27 | Wendy Achterberg | fix(express): spread express stops over the runs of the morning |  |
| `4545a4c` | 2026-08-28 | Kasper Nyholt | test(api): drivers and vehicles available on a day |  |
| `92186ae` | 2026-09-01 | Kasper Nyholt | contracts: pin the consumed order schemas; publish the dispatch event schemas and AsyncAPI document |  |
| `f03b1a5` | 2026-09-02 | Wendy Achterberg | docs: express rules in the README |  |
| `3274671` | 2026-08-31 | Wendy Achterberg | refactor: name the out-for-delivery event in the past tense; DomainException with one constructor |  |
| `b882eda` | 2026-09-03 | Kasper Nyholt | fix: one route per driver and day |  |
| `dbbb76d` | 2026-09-03 | Kasper Nyholt | test: every delivery zone; the order consumer's queue, bindings and dead-lettering |  |
| `4b51436` | 2026-09-04 | Kasper Nyholt | release 0.3.0 | tag `v0.3.0` |
