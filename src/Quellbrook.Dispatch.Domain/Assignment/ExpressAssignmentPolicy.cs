using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>Same-day express service (ADR 0004).</summary>
public sealed class ExpressAssignmentPolicy(
    TimeZoneInfo depotTimeZone,
    double minimumShiftHours = StandardAssignmentPolicy.DefaultMinimumShiftHours)
{
    public static readonly TimeOnly CutOff = new(14, 0);
    public const double CapacityBuffer = 0.1;
    public const double HoursBeforeBreak = 4.5;

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

        if (time > CutOff || local.DayOfWeek == DayOfWeek.Saturday || local.DayOfWeek == DayOfWeek.Sunday)
        {
            return AssignmentDecision.None($"The express cut-off ({CutOff:HH:mm} on working days) has passed.");
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

            var sameZone = route.Zone == consignment.Zone;
            if (!sameZone)
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

            if (time < driver.ShiftStart || time >= driver.ShiftEnd.AddHours(-1))
            {
                continue;
            }

            var hoursWorked = (time - driver.ShiftStart).TotalHours;
            if (hoursWorked >= HoursBeforeBreak && route.StartedAt is null && !route.Express)
            {
                continue;
            }

            if (remaining - consignment.TotalWeightGrams < candidate.Vehicle.CapacityGrams * CapacityBuffer && !route.Express)
            {
                continue;
            }

            var score = 0;
            if (route.Express)
            {
                score += 100;
            }

            if (sameZone)
            {
                score += 50;
            }

            score -= route.Stops.Count * 5;
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
}
