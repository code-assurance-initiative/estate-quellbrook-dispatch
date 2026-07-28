namespace Quellbrook.Dispatch.Domain.Consignments;

public interface IConsignmentRepository
{
    Task<Consignment?> FindAsync(ConsignmentId id, CancellationToken cancellationToken);

    Task<Consignment?> FindByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Consignment>> OnRouteAsync(Routes.RouteId routeId, CancellationToken cancellationToken);

    void Add(Consignment consignment);
}
