using Microsoft.EntityFrameworkCore;
using Quellbrook.Dispatch.Api.Security;
using Quellbrook.Dispatch.Infrastructure.Persistence;

namespace Quellbrook.Dispatch.Api.Endpoints;

/// <summary>Drivers of a depot who have no route on a day yet, for the dispatch board's route planner.</summary>
public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/drivers/available", AvailableAsync).RequireAuthorization(AuthorizationPolicies.ReadDispatch);
        return app;
    }

    private static async Task<IResult> AvailableAsync(DateOnly date, string depot, DispatchDbContext db, CancellationToken cancellationToken)
    {
        var planned = await db.Routes
            .Where(route => route.ServiceDate == date)
            .Select(route => new { route.DriverId, route.VehicleId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var busyDrivers = planned.Select(route => route.DriverId).ToList();
        var busyVehicles = planned.Select(route => route.VehicleId).ToList();
        var normalisedDepot = depot.Trim().ToUpperInvariant();

        var drivers = await db.Drivers
            .Where(driver => driver.Active && driver.Depot == normalisedDepot && !busyDrivers.Contains(driver.Id))
            .OrderBy(driver => driver.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var vehicles = await db.Vehicles
            .Where(vehicle => vehicle.Depot == normalisedDepot && !busyVehicles.Contains(vehicle.Id))
            .OrderBy(vehicle => vehicle.Registration)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new
        {
            drivers = drivers.Select(driver => new
            {
                id = driver.Id.Value,
                driver.DisplayName,
                licence = driver.Licence.ToString(),
                driver.ShiftStart,
                driver.ShiftEnd,
            }),
            vehicles = vehicles.Select(vehicle => new
            {
                id = vehicle.Id.Value,
                vehicle.Registration,
                kind = vehicle.Kind.ToString(),
                vehicle.CapacityGrams,
            }),
        });
    }
}
