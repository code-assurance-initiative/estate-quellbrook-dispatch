namespace Quellbrook.Dispatch.Contracts.IntegrationEvents;

/// <summary>
/// Published when a driver records a delivery (routing key <c>dispatch.consignment-delivered.v1</c>). Schema:
/// contracts/events/consignment-delivered.v1.schema.json.
/// </summary>
public sealed record ConsignmentDeliveredV1(Guid ConsignmentId, Guid OrderId, string Proof, DateTimeOffset DeliveredAt)
{
    public const string EventType = "dispatch.consignment-delivered.v1";
}
