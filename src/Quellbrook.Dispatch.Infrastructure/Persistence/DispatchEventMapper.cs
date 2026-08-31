using Quellbrook.Dispatch.Contracts.IntegrationEvents;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

internal sealed record IntegrationMessage(Guid MessageId, string EventType, object Payload);

/// <summary>Maps domain events to the published dispatch contracts.</summary>
internal static class DispatchEventMapper
{
    public static IntegrationMessage ToIntegrationMessage(IDomainEvent domainEvent) => domainEvent switch
    {
        ConsignmentSentOutForDelivery outForDelivery => new IntegrationMessage(
            Guid.CreateVersion7(outForDelivery.OccurredAt),
            ConsignmentSentOutForDeliveryV1.EventType,
            new ConsignmentSentOutForDeliveryV1(
                outForDelivery.ConsignmentId.Value,
                outForDelivery.OrderId,
                outForDelivery.RouteId.Value,
                outForDelivery.OccurredAt)),
        ConsignmentDelivered delivered => new IntegrationMessage(
            Guid.CreateVersion7(delivered.OccurredAt),
            ConsignmentDeliveredV1.EventType,
            new ConsignmentDeliveredV1(
                delivered.ConsignmentId.Value,
                delivered.OrderId,
                ProofName(delivered.Proof),
                delivered.OccurredAt)),
        _ => throw new InvalidOperationException($"No integration event for {domainEvent.GetType().Name}."),
    };

    public static string ProofName(DeliveryProof proof) => proof switch
    {
        DeliveryProof.Signature => "signature",
        DeliveryProof.Photo => "photo",
        DeliveryProof.SafePlace => "safe-place",
        _ => throw new ArgumentOutOfRangeException(nameof(proof), proof, null),
    };
}
