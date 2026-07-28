using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Consignments;

public sealed record ConsignmentOutForDelivery(
    ConsignmentId ConsignmentId,
    Guid OrderId,
    RouteId RouteId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ConsignmentDelivered(
    ConsignmentId ConsignmentId,
    Guid OrderId,
    DeliveryProof Proof,
    DateTimeOffset OccurredAt) : IDomainEvent;
