namespace Quellbrook.Dispatch.Application.Queries;

/// <summary>The read side of the dispatch board and of the gateway's shipment view.</summary>
public interface IDispatchQueries
{
    Task<IReadOnlyList<RouteBoardEntry>> BoardAsync(DateOnly serviceDate, CancellationToken cancellationToken);

    Task<ConsignmentView?> ForOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
