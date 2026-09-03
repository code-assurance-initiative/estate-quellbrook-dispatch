using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Application.Routes;

public sealed record PlanRouteCommand(string Depot, string Zone, DateOnly ServiceDate, Guid DriverId, Guid VehicleId, bool Express);

public sealed class PlanRouteHandler(IFleetRepository fleet, IRouteRepository routes, IUnitOfWork unitOfWork, TimeProvider time)
{
    public async Task<OperationResult<RouteId>> HandleAsync(PlanRouteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var driver = await fleet.FindDriverAsync(new DriverId(command.DriverId), cancellationToken).ConfigureAwait(false);
        var vehicle = await fleet.FindVehicleAsync(new VehicleId(command.VehicleId), cancellationToken).ConfigureAwait(false);
        if (driver is null || vehicle is null)
        {
            return OperationResult.NotFound<RouteId>("The driver or the vehicle does not exist.");
        }

        if (await routes.DriverHasRouteAsync(driver.Id, command.ServiceDate, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult.Conflict<RouteId>($"{driver.DisplayName} already has a route on {command.ServiceDate:yyyy-MM-dd}.");
        }

        try
        {
            var route = Route.Plan(
                new RouteId(Guid.CreateVersion7(time.GetUtcNow())),
                command.Depot,
                command.Zone,
                command.ServiceDate,
                driver,
                vehicle,
                command.Express);
            routes.Add(route);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return OperationResult.Succeeded(route.Id);
        }
        catch (DomainException exception)
        {
            return OperationResult.Invalid<RouteId>(exception.Message);
        }
    }
}
