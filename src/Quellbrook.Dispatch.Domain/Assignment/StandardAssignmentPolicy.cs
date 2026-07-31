using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>
/// Standard service: the consignment goes on a planned standard route of its own zone for the service date, on the
/// route whose vehicle it fills best (least capacity left over), so that space for large consignments is kept.
/// </summary>
public sealed class StandardAssignmentPolicy(double minimumShiftHours = StandardAssignmentPolicy.DefaultMinimumShiftHours)
{
    /// <summary>By default a driver needs at least this long a shift to take on another stop.</summary>
    public const double DefaultMinimumShiftHours = 4;

    public AssignmentDecision Choose(Consignment consignment, IReadOnlyList<RouteCandidate> candidates, DateOnly serviceDate)
    {
        ArgumentNullException.ThrowIfNull(consignment);
        ArgumentNullException.ThrowIfNull(candidates);
        RouteCandidate? best = null;
        foreach (var candidate in candidates)
        {
            var route = candidate.Route;
            if (route.Status != RouteStatus.Planned || route.Express || route.ServiceDate != serviceDate || route.Zone != consignment.Zone)
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

            if (best is null || remaining < best.RemainingGrams)
            {
                best = candidate;
            }
        }

        return best is null
            ? AssignmentDecision.None($"No standard route in zone {consignment.Zone} on {serviceDate:yyyy-MM-dd} has room for {consignment.TotalWeightGrams} g.")
            : AssignmentDecision.To(best.Route.Id);
    }
}
