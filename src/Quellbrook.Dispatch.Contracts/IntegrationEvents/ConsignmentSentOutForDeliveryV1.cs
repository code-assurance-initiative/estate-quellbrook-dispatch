namespace Quellbrook.Dispatch.Contracts.IntegrationEvents;

/// <summary>
/// Published when the route carrying a consignment starts (routing key
/// <c>dispatch.consignment-out-for-delivery.v1</c>). Schema: contracts/events/consignment-out-for-delivery.v1.schema.json.
/// </summary>
public sealed record ConsignmentSentOutForDeliveryV1(
    Guid ConsignmentId,
    Guid OrderId,
    Guid RouteId,
    DateTimeOffset OutForDeliveryAt)
{
    public const string EventType = "dispatch.consignment-out-for-delivery.v1";
}
