using Microsoft.Extensions.Logging;
using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Consignments;

namespace Quellbrook.Dispatch.Application.Intake;

/// <summary>Turns an announced order into a consignment waiting for a route. Safe to run twice for one order.</summary>
public sealed partial class OrderPlacedHandler(
    IConsignmentRepository consignments,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<OrderPlacedHandler> logger)
{
    public async Task HandleAsync(OrderPlacedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (await consignments.FindByOrderAsync(message.OrderId, cancellationToken).ConfigureAwait(false) is not null)
        {
            LogAlreadyReceived(message.OrderId);
            return;
        }

        var now = time.GetUtcNow();
        var consignment = Consignment.Receive(
            new ConsignmentId(Guid.CreateVersion7(now)),
            message.OrderId,
            message.ServiceLevel == "express" ? ServiceLevel.Express : ServiceLevel.Standard,
            message.Consignee.Address.CountryCode,
            message.Consignee.Address.PostalCode,
            [.. message.Parcels.Select(parcel => parcel.WeightGrams)],
            now);
        consignments.Add(consignment);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogReceived(message.OrderId, consignment.Zone);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consignment for order {OrderId} received for zone {Zone}")]
    private partial void LogReceived(Guid orderId, string zone);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Order {OrderId} already has a consignment")]
    private partial void LogAlreadyReceived(Guid orderId);
}
