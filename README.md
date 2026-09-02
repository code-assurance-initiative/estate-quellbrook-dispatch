# Quellbrook Dispatch

The dispatch service of Quellbrook Freight's operations platform. When the order service announces an order
(`orders.order-placed.v1`), dispatch turns it into a **consignment** waiting in the delivery zone of its postal code.
Dispatchers plan **routes** — one driver and one vehicle in one zone on one day — and assign consignments to them;
express consignments received before the 14:00 cut-off go on a route of the same day (ADR 0004). When a
driver leaves the depot the route starts and its consignments are out for delivery, and the driver records
each delivery. Dispatch announces both moments (`dispatch.consignment-out-for-delivery.v1`,
`dispatch.consignment-delivered.v1`), which the notifier turns into messages for the consignee.

**Owner:** the Fleet team (`#team-fleet`). **System:** Quellbrook operations platform (see `catalog-info.yaml`).

## API

| Method and path | Scope | What it does |
|---|---|---|
| `POST /fleet/drivers`, `POST /fleet/vehicles` | `fleet:admin` | Register a driver or a vehicle of a depot |
| `POST /routes` | `dispatch:write` | Plan a route |
| `GET /routes?date=` | `dispatch:read` | The dispatch board: every route of a day with its stops |
| `POST /routes/{id}/start` | `dispatch:write` | The route leaves the depot |
| `POST /consignments/{id}/assignment` | `dispatch:write` | Assign a waiting consignment to the best route |
| `POST /consignments/{id}/delivery` | `dispatch:write` | Record a delivery and its proof |
| `GET /consignments/by-order/{orderId}` | `dispatch:read` | A consignment's status, for the gateway's shipment view |
| `GET /drivers/available?date=&depot=` | `dispatch:read` | Drivers and vehicles of a depot without a route on a day |
| `GET /health/live`, `GET /health/ready` | none | Liveness and readiness probes |

The contract is `contracts/openapi.yaml`; the event schemas are in `contracts/events/` and `contracts/asyncapi.yaml`,
the consumed order schemas pinned in `contracts/consumed/orders/`. Only the gateway holds a token with these scopes.

## Messages

| Direction | Routing key | Handled by |
|---|---|---|
| consumed | `orders.order-placed.v1` | creates the consignment (idempotent: inbox + one consignment per order) |
| consumed | `orders.order-cancelled.v1` | drops the consignment and its stop, unless it has left the depot |
| published | `dispatch.consignment-out-for-delivery.v1` | when a route starts, per consignment |
| published | `dispatch.consignment-delivered.v1` | when a delivery is recorded |

Consumption goes through an inbox and publication through an outbox (ADR 0003); both live in the service's
database, so a message is processed exactly once and an event is published if and only if its change was committed.

## Express

Express consignments follow ADR 0004: before the 14:00 cut-off (depot time, working days) they go on a route of the
same day — an express run first, a standard route that has not left and keeps a 10 % buffer otherwise; an adjacent
zone's route may take a small one; heavy ones need a rigid vehicle and a C1 driver; a driver due their break takes no
more. The rules are `ExpressAssignmentPolicy` with `ExpressCutOff` and `DriverHours` in
`src/Quellbrook.Dispatch.Domain/Assignment`.

## Build and run

Requires the .NET 10 SDK (`global.json`), a PostgreSQL 16 database and a RabbitMQ broker.

```sh
dotnet tool restore
dotnet build Quellbrook.Dispatch.slnx
export ConnectionStrings__Dispatch="Host=localhost;Database=dispatch;Username=dispatch"
dotnet ef database update --project src/Quellbrook.Dispatch.Infrastructure
dotnet run --project src/Quellbrook.Dispatch.Api
```

## Configuration

| Setting | Meaning |
|---|---|
| `ConnectionStrings__Dispatch` | PostgreSQL connection string (a Kubernetes Secret in production) |
| `Authentication__Authority`, `Authentication__Audience` | the OpenID Connect issuer and this API's audience |
| `RabbitMq__Uri`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__Exchange` | the broker; credentials from a Secret |
| `Outbox__PollInterval`, `Outbox__BatchSize`, `Outbox__Retention` | outbox relay tuning |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | where traces, metrics and logs are exported, when set |

## Testing

```sh
dotnet test Quellbrook.Dispatch.slnx
```

`tests/Quellbrook.Dispatch.UnitTests` covers the consignment and route rules, the delivery zones, the assignment
policy, the handlers (over in-memory SQLite), the inbox, the consumer's acknowledgement rules, the outbox relay and the
broker clients. `tests/Quellbrook.Dispatch.IntegrationTests` hosts the real API with `WebApplicationFactory` over
in-memory SQLite and an in-process token issuer; the consumer and the relay do not run there.

## Architecture

Domain, application, infrastructure and API projects with dependencies pointing inward; endpoints call application
handlers only (ADR 0002). See [docs/architecture.md](docs/architecture.md) and [docs/adr](docs/adr).

## Deployment

A published GitHub release builds the image and the migrations bundle (`.github/workflows/release.yml`); the manual
`deploy.yml` workflow migrates the database and rolls the image out to the `quellbrook-dispatch` namespace
(`deploy/k8s/`), rolling back when the new pods do not become ready.

## Contributing

Open a pull request against `main`. CI must be green and CodeQL must report no new alerts. Schema changes come with
an EF Core migration. Record user-visible changes in `CHANGELOG.md`.

## Licence

MIT; see [LICENSE](LICENSE).
