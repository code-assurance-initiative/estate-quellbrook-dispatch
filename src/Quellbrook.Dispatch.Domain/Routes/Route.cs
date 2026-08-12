using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;

namespace Quellbrook.Dispatch.Domain.Routes;

/// <summary>One driver and one vehicle delivering in one zone on one day.</summary>
public sealed class Route : AggregateRoot
{
    private readonly List<RouteStop> _stops = [];

    private Route(RouteId id, string depot, string zone, DateOnly serviceDate, DriverId driverId, VehicleId vehicleId, bool express)
    {
        Id = id;
        Depot = depot;
        Zone = zone;
        ServiceDate = serviceDate;
        DriverId = driverId;
        VehicleId = vehicleId;
        Express = express;
        Status = RouteStatus.Planned;
    }

    public RouteId Id { get; private set; }

    public string Depot { get; private set; }

    public string Zone { get; private set; }

    public DateOnly ServiceDate { get; private set; }

    public DriverId DriverId { get; private set; }

    public VehicleId VehicleId { get; private set; }

    /// <summary>A same-day express run, planned in addition to the zone's standard route.</summary>
    public bool Express { get; private set; }

    public RouteStatus Status { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public IReadOnlyList<RouteStop> Stops => _stops;

    public int LoadGrams => _stops.Sum(stop => stop.WeightGrams);

    public static Route Plan(RouteId id, string depot, string zone, DateOnly serviceDate, Driver driver, Vehicle vehicle, bool express)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentException.ThrowIfNullOrWhiteSpace(zone);
        if (driver.Licence < vehicle.RequiredLicence)
        {
            throw new DomainException($"Driver {driver.DisplayName} may not drive {vehicle.Registration}.");
        }

        if (!driver.Active)
        {
            throw new DomainException($"Driver {driver.DisplayName} is not active.");
        }

        return new Route(id, depot, zone, serviceDate, driver.Id, vehicle.Id, express);
    }

    /// <summary>Adds a consignment as the last stop; the caller has checked the vehicle's capacity.</summary>
    public void AddStop(Consignment consignment)
    {
        ArgumentNullException.ThrowIfNull(consignment);
        EnsurePlanned();
        if (_stops.Any(stop => stop.ConsignmentId == consignment.Id))
        {
            throw new DomainException($"Consignment {consignment.Id} is already on route {Id}.");
        }

        _stops.Add(new RouteStop(consignment.Id, consignment.TotalWeightGrams, _stops.Count + 1));
        consignment.AssignTo(Id);
    }

    /// <summary>Takes a cancelled consignment off the route and renumbers the stops after it.</summary>
    public void RemoveStop(ConsignmentId consignmentId)
    {
        EnsurePlanned();
        var removed = _stops.RemoveAll(stop => stop.ConsignmentId == consignmentId);
        if (removed == 0)
        {
            throw new DomainException($"Consignment {consignmentId} is not on route {Id}.");
        }

        for (var i = 0; i < _stops.Count; i++)
        {
            _stops[i] = _stops[i] with { Sequence = i + 1 };
        }
    }

    public void Start(IEnumerable<Consignment> consignments, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(consignments);
        EnsurePlanned();
        if (_stops.Count == 0)
        {
            throw new DomainException($"Route {Id} has no stops.");
        }

        Status = RouteStatus.Started;
        StartedAt = at;
        foreach (var consignment in consignments.Where(consignment => _stops.Any(stop => stop.ConsignmentId == consignment.Id)))
        {
            consignment.MarkOutForDelivery(at);
        }
    }

    private void EnsurePlanned()
    {
        if (Status != RouteStatus.Planned)
        {
            throw new DomainException($"Route {Id} has already started.");
        }
    }
}
