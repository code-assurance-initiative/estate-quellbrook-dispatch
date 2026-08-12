namespace Quellbrook.Dispatch.Application.Intake;

/// <summary>Dispatch's copy of the order service's <c>orders.order-cancelled.v1</c> payload.</summary>
public sealed record OrderCancelledMessage(Guid OrderId, string Reason, DateTimeOffset CancelledAt)
{
    public const string EventType = "orders.order-cancelled.v1";
}
