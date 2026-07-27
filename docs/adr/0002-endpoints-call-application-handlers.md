# 2. Endpoints call application handlers; persistence only through repositories

- Status: Accepted
- Date: 2026-07-27
- Deciders: Kasper Nyholt, Wendy Achterberg

## Context

Dispatch rules (zones, capacity, licences, shifts, route lifecycle) must hold whichever way a change arrives: an HTTP
call from the gateway, a consumed order event, or a future driver app. If endpoints query and change the database
themselves, each entry point grows its own copy of the rules and the board shows what the database says rather than
what the domain allows.

## Decision

Four projects with dependencies pointing inward: Domain, Application, Infrastructure, Api. HTTP endpoints validate the
request shape and call an application handler or a query service (`IDispatchQueries`); they never take the
`DispatchDbContext` or a repository directly. Application handlers reach persistence only through the repository
interfaces of the domain and `IUnitOfWork`. Queries for screens go through `IDispatchQueries`, implemented in
Infrastructure.

## Consequences

- Positive: one place per rule, testable without HTTP; the API can be replaced or doubled (driver app) without
  touching the rules; reads for screens are explicit and can be optimised in one place.
- Negative: a small read for a screen needs a query method and a view type instead of a quick LINQ query in the
  endpoint; more types for simple cases.
