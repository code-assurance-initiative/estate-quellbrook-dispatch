using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Fleet;

namespace Quellbrook.Dispatch.Application.Fleet;

public sealed record RegisterDriverCommand(string DisplayName, string Depot, LicenceCategory Licence, TimeOnly ShiftStart, TimeOnly ShiftEnd);

public sealed record RegisterVehicleCommand(string Registration, string Depot, VehicleKind Kind, int CapacityGrams);

/// <summary>Adds drivers and vehicles to a depot's fleet.</summary>
public sealed class RegisterFleetHandlers(IFleetRepository fleet, IUnitOfWork unitOfWork, TimeProvider time)
{
    public async Task<OperationResult<DriverId>> RegisterDriverAsync(RegisterDriverCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var driver = Driver.Register(
                new DriverId(Guid.CreateVersion7(time.GetUtcNow())),
                command.DisplayName,
                command.Depot,
                command.Licence,
                command.ShiftStart,
                command.ShiftEnd);
            fleet.Add(driver);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return OperationResult.Succeeded(driver.Id);
        }
        catch (DomainException exception)
        {
            return OperationResult.Invalid<DriverId>(exception.Message);
        }
    }

    public async Task<OperationResult<VehicleId>> RegisterVehicleAsync(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var vehicle = Vehicle.Register(
                new VehicleId(Guid.CreateVersion7(time.GetUtcNow())),
                command.Registration,
                command.Depot,
                command.Kind,
                command.CapacityGrams);
            fleet.Add(vehicle);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return OperationResult.Succeeded(vehicle.Id);
        }
        catch (DomainException exception)
        {
            return OperationResult.Invalid<VehicleId>(exception.Message);
        }
    }
}
