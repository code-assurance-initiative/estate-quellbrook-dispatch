# Architecture — dispatch service

## Context

Dispatch is one of five parts of the Quellbrook operations platform. It learns about orders from the order
service's events, is driven by dispatchers through the operator console and the gateway, and tells the notifier when
parcels are on their way and delivered. Traffic between services inside the cluster goes through the Linkerd service
mesh (mutual TLS); the service listens on plain HTTP inside its pod and is reachable only from the gateway's namespace.

```mermaid
C4Context
  title System context — Quellbrook operations platform
  Person(dispatcher, "Dispatcher", "Plans routes and assigns consignments")
  Person(driver, "Driver", "Delivers the parcels")
  System_Boundary(platform, "Quellbrook operations platform") {
    System(web, "Operator console", "estate-quellbrook-web")
    System(gateway, "API gateway / BFF", "estate-quellbrook-gateway")
    System(orders, "Order service", "estate-quellbrook-orders")
    System(dispatch, "Dispatch service", "this repository")
    System(notifier, "Notifier", "estate-quellbrook-notifier")
  }
  Rel(dispatcher, web, "Uses", "HTTPS")
  Rel(web, gateway, "Calls /api", "HTTPS/JSON")
  Rel(gateway, dispatch, "Board, assignment, routes", "HTTP/JSON, mesh mTLS")
  Rel(orders, dispatch, "orders.order-placed.v1", "RabbitMQ")
  Rel(dispatch, notifier, "dispatch.* events", "RabbitMQ")
  Rel(driver, gateway, "Records deliveries (handheld)", "HTTPS")
```

## Containers

```mermaid
C4Container
  title Containers — dispatch service
  System_Ext(gateway, "API gateway / BFF", "estate-quellbrook-gateway")
  Container_Boundary(service, "Dispatch service") {
    Container(api, "Dispatch API", "ASP.NET Core, .NET 10", "Fleet, routes, assignment, deliveries; order-event consumer; outbox relay")
    ContainerDb(db, "Dispatch database", "PostgreSQL 16", "consignments, routes, route_stops, drivers, vehicles, inbox_messages, outbox_messages")
  }
  ContainerQueue(broker, "Event broker", "RabbitMQ", "topic exchange quellbrook.events; queue dispatch.order-events")
  System_Ext(idp, "Identity provider", "OpenID Connect")
  Rel(gateway, api, "Calls", "HTTP/JSON over mesh mTLS, bearer token with dispatch:* / fleet:admin")
  Rel(api, db, "Reads and writes", "EF Core / Npgsql")
  Rel(broker, api, "Delivers orders.order-placed.v1", "AMQPS")
  Rel(api, broker, "Publishes dispatch.*.v1 from the outbox", "AMQPS")
  Rel(api, idp, "Fetches token signing keys", "HTTPS")
```

## Inside the service

| Project | Responsibility | Depends on |
|---|---|---|
| `Quellbrook.Dispatch.Domain` | consignments, routes, drivers, vehicles, delivery zones, assignment policy | nothing |
| `Quellbrook.Dispatch.Contracts` | the published integration-event contracts | nothing |
| `Quellbrook.Dispatch.Application` | command handlers, the consumed-message handlers, query interfaces | Domain, Contracts |
| `Quellbrook.Dispatch.Infrastructure` | EF Core persistence and migrations, inbox, outbox and relay, RabbitMQ consumer and publisher | Application |
| `Quellbrook.Dispatch.Api` | minimal-API endpoints, authentication and authorization, hosting | Infrastructure |

Endpoints call application handlers and `IDispatchQueries` only (ADR 0002). A consumed message runs through
`InboxProcessor`: its id is recorded in the same transaction as the handler's changes (ADR 0003).

```mermaid
sequenceDiagram
  participant B as RabbitMQ
  participant C as OrderEventsConsumer
  participant I as InboxProcessor
  participant H as OrderPlacedHandler
  participant D as PostgreSQL
  B->>C: orders.order-placed.v1 (message-id)
  C->>I: process(message-id, body)
  I->>D: seen this id? (no)
  I->>H: handle
  H->>D: one consignment per order
  I->>D: INSERT inbox row + changes, COMMIT
  C->>B: ack
```

## Message contracts

| Routing key | Payload | Direction |
|---|---|---|
| `orders.order-placed.v1` | order id, service level, consignee address (postal code, country), parcel weights — dispatch's own copy of the fields it uses (`OrderPlacedMessage`) | consumed |
| `dispatch.consignment-out-for-delivery.v1` | consignment id, order id, route id, time | published |
| `dispatch.consignment-delivered.v1` | consignment id, order id, proof (signature, photo, safe place), time | published |
