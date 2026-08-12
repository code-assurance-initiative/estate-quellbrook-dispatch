using Microsoft.Extensions.Logging;
using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Application.Intake;

/// <summary>Drops the consignment of a cancelled order, and its stop if it was on a route that has not left yet.</summary>
public sealed partial class OrderCancelledHandler(
    IConsignmentRepository consignments,
    IRouteRepository routes,
    IUnitOfWork unitOfWork,
    ILogger<OrderCancelledHandler> logger)
{
    public async Task HandleAsync(OrderCancelledMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var consignment = await consignments.FindByOrderAsync(message.OrderId, cancellationToken).ConfigureAwait(false);
        if (consignment is null || consignment.Status == ConsignmentStatus.Cancelled)
        {
            return;
        }

        if (consignment.Status is ConsignmentStatus.OutForDelivery or ConsignmentStatus.Delivered)
        {
            LogTooLate(message.OrderId, consignment.Status);
            return;
        }

        if (consignment.RouteId is { } routeId && await routes.FindAsync(routeId, cancellationToken).ConfigureAwait(false) is { } route)
        {
            route.RemoveStop(consignment.Id);
        }

        consignment.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order {OrderId} was cancelled but its consignment is already {Status}; the driver returns it")]
    private partial void LogTooLate(Guid orderId, ConsignmentStatus status);
}
