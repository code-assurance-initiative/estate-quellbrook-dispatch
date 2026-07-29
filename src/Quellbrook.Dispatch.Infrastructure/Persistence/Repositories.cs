using Microsoft.EntityFrameworkCore;
using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

public sealed class EfConsignmentRepository(DispatchDbContext db) : IConsignmentRepository
{
    public Task<Consignment?> FindAsync(ConsignmentId id, CancellationToken cancellationToken) =>
        db.Consignments.SingleOrDefaultAsync(consignment => consignment.Id == id, cancellationToken);

    public Task<Consignment?> FindByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Consignments.SingleOrDefaultAsync(consignment => consignment.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<Consignment>> OnRouteAsync(RouteId routeId, CancellationToken cancellationToken) =>
        await db.Consignments.Where(consignment => consignment.RouteId == routeId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Consignment consignment) => db.Consignments.Add(consignment);
}

public sealed class EfRouteRepository(DispatchDbContext db) : IRouteRepository
{
    public Task<Route?> FindAsync(RouteId id, CancellationToken cancellationToken) =>
        db.Routes.SingleOrDefaultAsync(route => route.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RouteCandidate>> CandidatesAsync(DateOnly serviceDate, CancellationToken cancellationToken)
    {
        var routes = await db.Routes
            .Where(route => route.ServiceDate == serviceDate && route.Status == RouteStatus.Planned)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var driverIds = routes.Select(route => route.DriverId).Distinct().ToList();
        var vehicleIds = routes.Select(route => route.VehicleId).Distinct().ToList();
        var drivers = await db.Drivers.Where(driver => driverIds.Contains(driver.Id)).ToDictionaryAsync(driver => driver.Id, cancellationToken)
            .ConfigureAwait(false);
        var vehicles = await db.Vehicles.Where(vehicle => vehicleIds.Contains(vehicle.Id)).ToDictionaryAsync(vehicle => vehicle.Id, cancellationToken)
            .ConfigureAwait(false);
        return [.. routes.Select(route => new RouteCandidate(route, vehicles[route.VehicleId], drivers[route.DriverId]))];
    }

    public void Add(Route route) => db.Routes.Add(route);
}

public sealed class EfFleetRepository(DispatchDbContext db) : IFleetRepository
{
    public Task<Driver?> FindDriverAsync(DriverId id, CancellationToken cancellationToken) =>
        db.Drivers.SingleOrDefaultAsync(driver => driver.Id == id, cancellationToken);

    public Task<Vehicle?> FindVehicleAsync(VehicleId id, CancellationToken cancellationToken) =>
        db.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public void Add(Driver driver) => db.Drivers.Add(driver);

    public void Add(Vehicle vehicle) => db.Vehicles.Add(vehicle);
}
