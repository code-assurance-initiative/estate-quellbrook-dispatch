using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>Whether a driver can still take an express stop at a time of day (ADR 0004).</summary>
public static class DriverHours
{
    /// <summary>After this many hours without a started route the driver is due the mandatory break.</summary>
    public const double HoursBeforeBreak = 4.5;

    public static bool CanTakeExpressStop(Driver driver, Route route, TimeOnly time)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(route);
        var onShift = time >= driver.ShiftStart && time < driver.ShiftEnd.AddHours(-1);
        var dueBreak = (time - driver.ShiftStart).TotalHours >= HoursBeforeBreak && route.StartedAt is null && !route.Express;
        return onShift && !dueBreak;
    }
}
