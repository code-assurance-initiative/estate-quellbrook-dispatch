namespace Quellbrook.Dispatch.Application.Intake;

/// <summary>
/// Dispatch's copy of the order service's <c>orders.order-placed.v1</c> payload: only the fields dispatch uses
/// (contracts/consumed/orders/order-placed.v1.schema.json is the pinned schema). Unknown fields are ignored.
/// </summary>
public sealed record OrderPlacedMessage(
    Guid OrderId,
    string ServiceLevel,
    OrderPlacedMessage.ConsigneePart Consignee,
    IReadOnlyList<OrderPlacedMessage.ParcelPart> Parcels,
    DateTimeOffset PlacedAt)
{
    public const string EventType = "orders.order-placed.v1";

    public sealed record ConsigneePart(AddressPart Address);

    public sealed record AddressPart(string PostalCode, string CountryCode);

    public sealed record ParcelPart(int WeightGrams);
}
