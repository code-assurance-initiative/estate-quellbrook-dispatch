using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Consignments;

/// <summary>
/// The parcels of one order, as dispatch sees them: where they go, how heavy they are, and how far they got. Created
/// when the order service announces the order.
/// </summary>
public sealed class Consignment : AggregateRoot
{
    private Consignment(
        ConsignmentId id,
        Guid orderId,
        ServiceLevel serviceLevel,
        string countryCode,
        string postalCode,
        string zone,
        int parcelCount,
        int totalWeightGrams,
        DateTimeOffset receivedAt)
    {
        Id = id;
        OrderId = orderId;
        ServiceLevel = serviceLevel;
        CountryCode = countryCode;
        PostalCode = postalCode;
        Zone = zone;
        ParcelCount = parcelCount;
        TotalWeightGrams = totalWeightGrams;
        ReceivedAt = receivedAt;
        Status = ConsignmentStatus.AwaitingRoute;
    }

    public ConsignmentId Id { get; private set; }

    /// <summary>The order in the order service this consignment delivers.</summary>
    public Guid OrderId { get; private set; }

    public ServiceLevel ServiceLevel { get; private set; }

    public string CountryCode { get; private set; }

    public string PostalCode { get; private set; }

    public string Zone { get; private set; }

    public int ParcelCount { get; private set; }

    public int TotalWeightGrams { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public ConsignmentStatus Status { get; private set; }

    public RouteId? RouteId { get; private set; }

    public DateTimeOffset? OutForDeliveryAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public DeliveryProof? Proof { get; private set; }

    public static Consignment Receive(
        ConsignmentId id,
        Guid orderId,
        ServiceLevel serviceLevel,
        string countryCode,
        string postalCode,
        IReadOnlyCollection<int> parcelWeightsGrams,
        DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(parcelWeightsGrams);
        if (parcelWeightsGrams.Count == 0 || parcelWeightsGrams.Any(weight => weight <= 0))
        {
            throw new DomainException("A consignment has at least one parcel, each with a positive weight.");
        }

        return new Consignment(
            id,
            orderId,
            serviceLevel,
            countryCode,
            postalCode,
            DeliveryZones.ZoneFor(countryCode, postalCode),
            parcelWeightsGrams.Count,
            parcelWeightsGrams.Sum(),
            receivedAt);
    }

    public void AssignTo(RouteId routeId)
    {
        EnsureStatus(ConsignmentStatus.AwaitingRoute, "assigned to a route");
        RouteId = routeId;
        Status = ConsignmentStatus.Assigned;
    }

    public void MarkOutForDelivery(DateTimeOffset at)
    {
        EnsureStatus(ConsignmentStatus.Assigned, "sent out for delivery");
        Status = ConsignmentStatus.OutForDelivery;
        OutForDeliveryAt = at;
        Raise(new ConsignmentOutForDelivery(Id, OrderId, RouteId ?? throw new InvalidOperationException("Assigned without a route."), at));
    }

    public void RecordDelivery(DeliveryProof proof, DateTimeOffset at)
    {
        EnsureStatus(ConsignmentStatus.OutForDelivery, "delivered");
        Status = ConsignmentStatus.Delivered;
        DeliveredAt = at;
        Proof = proof;
        Raise(new ConsignmentDelivered(Id, OrderId, proof, at));
    }

    private void EnsureStatus(ConsignmentStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException($"Consignment {Id} is {Status} and cannot be {action}.");
        }
    }
}
