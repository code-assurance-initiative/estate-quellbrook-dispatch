using Microsoft.EntityFrameworkCore;
using Quellbrook.Dispatch.Application.Queries;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

public sealed class DispatchQueries(DispatchDbContext db) : IDispatchQueries
{
    public async Task<IReadOnlyList<RouteBoardEntry>> BoardAsync(DateOnly serviceDate, CancellationToken cancellationToken)
    {
        var routes = await db.Routes.AsNoTracking()
            .Where(route => route.ServiceDate == serviceDate)
            .OrderBy(route => route.Zone)
            .ThenBy(route => route.Express)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var drivers = await db.Drivers.AsNoTracking().ToDictionaryAsync(driver => driver.Id, cancellationToken).ConfigureAwait(false);
        var vehicles = await db.Vehicles.AsNoTracking().ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken).ConfigureAwait(false);
        var routeIds = routes.Select(route => route.Id).ToList();
        var consignments = await db.Consignments.AsNoTracking()
            .Where(consignment => consignment.RouteId != null && routeIds.Contains(consignment.RouteId.Value))
            .ToDictionaryAsync(consignment => consignment.Id, cancellationToken)
            .ConfigureAwait(false);

        return [.. routes.Select(route =>
        {
            var vehicle = vehicles[route.VehicleId];
            return new RouteBoardEntry(
                route.Id.Value,
                route.Depot,
                route.Zone,
                route.Express,
                route.Status.ToString(),
                drivers[route.DriverId].DisplayName,
                vehicle.Registration,
                vehicle.Kind.ToString(),
                vehicle.CapacityGrams,
                route.LoadGrams,
                [.. route.Stops.OrderBy(stop => stop.Sequence).Select(stop =>
                {
                    var consignment = consignments[stop.ConsignmentId];
                    return new RouteBoardStop(stop.Sequence, stop.ConsignmentId.Value, consignment.OrderId, consignment.PostalCode,
                        stop.WeightGrams, consignment.Status.ToString());
                })]);
        })];
    }

    public async Task<ConsignmentView?> ForOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var consignment = await db.Consignments.AsNoTracking()
            .SingleOrDefaultAsync(consignment => consignment.OrderId == orderId, cancellationToken)
            .ConfigureAwait(false);
        return consignment is null
            ? null
            : new ConsignmentView(
                consignment.Id.Value,
                consignment.OrderId,
                consignment.ServiceLevel.ToString(),
                consignment.Zone,
                consignment.Status.ToString(),
                consignment.RouteId?.Value,
                consignment.OutForDeliveryAt,
                consignment.DeliveredAt,
                consignment.Proof is { } proof ? DispatchEventMapper.ProofName(proof) : null);
    }
}
