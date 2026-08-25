namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>Express consignments are accepted for same-day delivery until 14:00 depot time on working days (ADR 0004).</summary>
public static class ExpressCutOff
{
    public static readonly TimeOnly Time = new(14, 0);

    public static bool HasPassed(DateTimeOffset depotTime) =>
        TimeOnly.FromDateTime(depotTime.DateTime) > Time
        || depotTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
