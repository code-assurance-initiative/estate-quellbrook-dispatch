# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-08-21

### Added
- Same-day express assignment (ADR 0004): cut-off 14:00, express runs first, adjacent zones for small consignments,
  rigid vehicles for heavy ones, the driver's break.
- Consignments of cancelled orders are dropped from their route (`orders.order-cancelled.v1`).
- `GET /drivers/available` for the dispatch board's route planner.

## [0.1.0] - 2026-08-07

### Added
- Consignments from `orders.order-placed.v1` through an inbox; delivery zones by postal code.
- Drivers, vehicles and routes; standard assignment of a consignment to the route its vehicle fits best.
- Starting a route and recording deliveries, announced as `dispatch.*` events through an outbox.
- Dispatch board and consignment status queries; container image, manifests, CI, CodeQL, release and deploy workflows.
