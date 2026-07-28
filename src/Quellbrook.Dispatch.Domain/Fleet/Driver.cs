using Quellbrook.Dispatch.Domain.Common;

namespace Quellbrook.Dispatch.Domain.Fleet;

/// <summary>A driver of a depot, with the licence they hold and the daily shift they work.</summary>
public sealed class Driver
{
    private Driver(DriverId id, string displayName, string depot, LicenceCategory licence, TimeOnly shiftStart, TimeOnly shiftEnd)
    {
        Id = id;
        DisplayName = displayName;
        Depot = depot;
        Licence = licence;
        ShiftStart = shiftStart;
        ShiftEnd = shiftEnd;
        Active = true;
    }

    public DriverId Id { get; private set; }

    /// <summary>First name and initial, as shown on the dispatch board.</summary>
    public string DisplayName { get; private set; }

    public string Depot { get; private set; }

    public LicenceCategory Licence { get; private set; }

    public TimeOnly ShiftStart { get; private set; }

    public TimeOnly ShiftEnd { get; private set; }

    public bool Active { get; private set; }

    public double ShiftHours => (ShiftEnd - ShiftStart).TotalHours;

    public static Driver Register(DriverId id, string displayName, string depot, LicenceCategory licence, TimeOnly shiftStart, TimeOnly shiftEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(depot);
        if (shiftEnd <= shiftStart)
        {
            throw new DomainException("A shift ends after it starts.");
        }

        return new Driver(id, displayName.Trim(), depot.Trim().ToUpperInvariant(), licence, shiftStart, shiftEnd);
    }

    public void Deactivate() => Active = false;
}
