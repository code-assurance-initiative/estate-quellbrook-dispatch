using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Domain;

public sealed class ExpressRulesTests
{
    private static readonly DateTimeOffset Wednesday = new(2026, 8, 26, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Day = DateOnly.FromDateTime(Wednesday.Date);
    private readonly ExpressAssignmentPolicy _policy = new(TimeZoneInfo.Utc);

    [Theory]
    [InlineData(13, 59, false)]
    [InlineData(14, 1, true)]
    public void TheCutOffIsTwoInTheAfternoonOnWorkingDays(int hour, int minute, bool passed) =>
        Assert.Equal(passed, ExpressCutOff.HasPassed(Wednesday.AddHours(hour).AddMinutes(minute)));

    [Fact]
    public void ThereIsNoExpressServiceAtTheWeekend() => Assert.True(ExpressCutOff.HasPassed(Wednesday.AddDays(3).AddHours(9)));

    [Theory]
    [InlineData(6, false)]
    [InlineData(9, true)]
    [InlineData(14, false)]
    public void ADriverTakesExpressStopsFromShiftStartUntilAnHourBeforeItsEnd(int hour, bool can)
    {
        var candidate = DispatchData.Candidate(express: true, date: Day);

        Assert.Equal(can, DriverHours.CanTakeExpressStop(candidate.Driver, candidate.Route, new TimeOnly(hour, 0)));
    }

    [Fact]
    public void ADriverDueTheirBreakTakesNoStopsForAStandardRouteThatHasNotLeft()
    {
        var standard = DispatchData.Candidate(date: Day);

        Assert.False(DriverHours.CanTakeExpressStop(standard.Driver, standard.Route, new TimeOnly(11, 45)));
        Assert.True(DriverHours.CanTakeExpressStop(standard.Driver, standard.Route, new TimeOnly(11, 15)));
    }

    [Fact]
    public void ASmallConsignmentMayGoOnAnAdjacentZonesRouteButALargeOneMayNot()
    {
        var adjacent = DispatchData.Candidate(zone: "DK-JUT-C", express: true, date: Day);
        var small = DispatchData.Consignment(serviceLevel: ServiceLevel.Express, weights: [1_000, 1_000]);
        var large = DispatchData.Consignment(serviceLevel: ServiceLevel.Express, weights: [1_000, 1_000, 1_000]);

        Assert.Equal(adjacent.Route.Id, _policy.Choose(small, [adjacent], Wednesday.AddHours(9)).RouteId);
        Assert.Null(_policy.Choose(large, [adjacent], Wednesday.AddHours(9)).RouteId);
    }

    [Fact]
    public void AHeavyConsignmentNeedsARigidVehicle()
    {
        var van = DispatchData.Candidate(express: true, date: Day);
        var rigid = DispatchData.Candidate(vehicle: DispatchData.Rigid(), express: true, date: Day);
        var heavy = DispatchData.Consignment(serviceLevel: ServiceLevel.Express, weights: 25_000);

        Assert.Equal(rigid.Route.Id, _policy.Choose(heavy, [van, rigid], Wednesday.AddHours(9)).RouteId);
    }

    [Fact]
    public void AStandardRouteKeepsATenPercentBufferForItsOwnStops()
    {
        var standard = DispatchData.Candidate(vehicle: DispatchData.Van(capacityGrams: 10_000), date: Day);
        var nearlyFull = DispatchData.Consignment(serviceLevel: ServiceLevel.Express, weights: 9_500);
        var fits = DispatchData.Consignment(serviceLevel: ServiceLevel.Express, weights: 8_000);

        Assert.Null(_policy.Choose(nearlyFull, [standard], Wednesday.AddHours(9)).RouteId);
        Assert.Equal(standard.Route.Id, _policy.Choose(fits, [standard], Wednesday.AddHours(9)).RouteId);
    }

    [Fact]
    public void AStandardConsignmentIsNotForTheExpressPolicy() =>
        Assert.Null(_policy.Choose(DispatchData.Consignment(), [DispatchData.Candidate(express: true, date: Day)], Wednesday.AddHours(9)).RouteId);

    [Fact]
    public void AnInactiveDriverOrAMissingLicenceIsSkipped()
    {
        var inactiveDriver = DispatchData.Driver();
        var inactive = DispatchData.Candidate(driver: inactiveDriver, express: true, date: Day);
        inactiveDriver.Deactivate();
        var unlicensed = DispatchData.Candidate(vehicle: DispatchData.Rigid(), driver: DispatchData.Driver(LicenceCategory.C1), express: true, date: Day)
            with { Driver = DispatchData.Driver(LicenceCategory.B) };

        Assert.Null(_policy.Choose(DispatchData.Consignment(serviceLevel: ServiceLevel.Express), [inactive, unlicensed], Wednesday.AddHours(9)).RouteId);
    }
}
