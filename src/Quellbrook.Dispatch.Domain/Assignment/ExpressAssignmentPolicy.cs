using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>Same-day express service (ADR 0004).</summary>
public sealed class ExpressAssignmentPolicy(
    TimeZoneInfo depotTimeZone,
    double minimumShiftHours = StandardAssignmentPolicy.DefaultMinimumShiftHours)
{
    public const int HeavyConsignmentGrams = 20_000;
    public const int SmallConsignmentParcels = 2;
    public const double CapacityBuffer = 0.1;

    public AssignmentDecision Choose(Consignment consignment, IReadOnlyList<RouteCandidate> candidates, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(consignment);
        ArgumentNullException.ThrowIfNull(candidates);
        var local = TimeZoneInfo.ConvertTime(now, depotTimeZone);
        var today = DateOnly.FromDateTime(local.DateTime);
        var time = TimeOnly.FromDateTime(local.DateTime);
        if (consignment.ServiceLevel != ServiceLevel.Express)
        {
            return AssignmentDecision.None("Not an express consignment.");
        }

        if (ExpressCutOff.HasPassed(local))
        {
            return AssignmentDecision.None($"The express cut-off ({ExpressCutOff.Time:HH:mm} on working days) has passed.");
        }

        RouteCandidate? best = null;
        var bestScore = int.MinValue;
        foreach (var candidate in candidates)
        {
            var route = candidate.Route;
            if (route.Status != RouteStatus.Planned || route.ServiceDate != today)
            {
                continue;
            }

            if (!ZoneFits(route, consignment, out var sameZone) || !VehicleFits(candidate, consignment))
            {
                continue;
            }

            var remaining = candidate.Vehicle.CapacityGrams - route.LoadGrams;
            if (remaining < consignment.TotalWeightGrams)
            {
                continue;
            }

            var driver = candidate.Driver;
            if (!driver.Active || driver.ShiftHours < minimumShiftHours)
            {
                continue;
            }

            if (driver.Licence < candidate.Vehicle.RequiredLicence)
            {
                continue;
            }

            if (!DriverHours.CanTakeExpressStop(driver, route, time))
            {
                continue;
            }

            if (remaining - consignment.TotalWeightGrams < candidate.Vehicle.CapacityGrams * CapacityBuffer && !route.Express)
            {
                continue;
            }

            var score = Score(route, sameZone);
            if (score > bestScore || (score == bestScore && best is not null && remaining < best.RemainingGrams))
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best is null
            ? AssignmentDecision.None($"No route can take express consignment {consignment.Id} today.")
            : AssignmentDecision.To(best.Route.Id);
    }

    /// <summary>Its own zone, or an adjacent one for a small consignment.</summary>
    private static bool ZoneFits(Route route, Consignment consignment, out bool sameZone)
    {
        sameZone = route.Zone == consignment.Zone;
        return sameZone
            || (DeliveryZones.AreAdjacent(route.Zone, consignment.Zone) && consignment.ParcelCount <= SmallConsignmentParcels);
    }

    /// <summary>A heavy consignment needs a rigid vehicle and a C1 driver.</summary>
    private static bool VehicleFits(RouteCandidate candidate, Consignment consignment) =>
        consignment.TotalWeightGrams <= HeavyConsignmentGrams
        || (candidate.Vehicle.Kind == VehicleKind.Rigid && candidate.Driver.Licence >= LicenceCategory.C1);

    /// <summary>Express runs first, then the consignment's own zone, then the route with fewer stops.</summary>
    private static int Score(Route route, bool sameZone) =>
        (route.Express ? 100 : 0) + (sameZone ? 50 : 0) - (route.Stops.Count * 10);
}
