namespace Quellbrook.Dispatch.Domain.Fleet;

public interface IFleetRepository
{
    Task<Driver?> FindDriverAsync(DriverId id, CancellationToken cancellationToken);

    Task<Vehicle?> FindVehicleAsync(VehicleId id, CancellationToken cancellationToken);

    void Add(Driver driver);

    void Add(Vehicle vehicle);
}
