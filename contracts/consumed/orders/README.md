# Consumed: order events

Pinned copies of the order service's event schemas that dispatch implements (`OrderPlacedMessage`,
`OrderCancelledMessage` in `src/Quellbrook.Dispatch.Application/Intake`). Source:
`estate-quellbrook-orders/contracts/events/`, release v0.3.0. Within v1 the producer only adds optional fields
(their ADR 0004); dispatch reads the order id, the service level, the consignee's postal code and country and the
parcel weights, and ignores everything else — including the consignee's contact details, which it does not store.
Update the copies when the producer announces a new version.
