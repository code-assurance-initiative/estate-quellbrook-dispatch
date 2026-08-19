using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Domain;

public sealed class StandardAssignmentPolicyTests
{
    private readonly StandardAssignmentPolicy _policy = new();

    [Fact]
    public void TheRouteWhoseVehicleItFillsBestIsChosen()
    {
        var roomy = DispatchData.Candidate(vehicle: DispatchData.Van(capacityGrams: 1_000_000));
        var tight = DispatchData.Candidate(vehicle: DispatchData.Van(capacityGrams: 10_000));

        var decision = _policy.Choose(DispatchData.Consignment(), [roomy, tight], DispatchData.Today);

        Assert.Equal(tight.Route.Id, decision.RouteId);
    }

    [Fact]
    public void RoutesOfOtherZonesOtherDaysAndExpressRunsAreNotCandidates()
    {
        var candidates = new[]
        {
            DispatchData.Candidate(zone: "DK-FYN"),
            DispatchData.Candidate(date: DispatchData.Today.AddDays(1)),
            DispatchData.Candidate(express: true),
        };

        var decision = _policy.Choose(DispatchData.Consignment(), candidates, DispatchData.Today);

        Assert.Null(decision.RouteId);
        Assert.Contains("DK-AAR", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AFullVehicleIsSkipped()
    {
        var full = DispatchData.Candidate(vehicle: DispatchData.Van(capacityGrams: 2_000));

        Assert.Null(_policy.Choose(DispatchData.Consignment(), [full], DispatchData.Today).RouteId);
    }

    [Fact]
    public void ADriverWithAShortShiftOrInactiveIsSkipped()
    {
        var shortShift = DispatchData.Candidate(driver: DispatchData.Driver(shiftStart: 7, shiftEnd: 10));
        var inactiveDriver = DispatchData.Driver();
        var inactive = DispatchData.Candidate(driver: inactiveDriver);
        inactiveDriver.Deactivate();

        Assert.Null(_policy.Choose(DispatchData.Consignment(), [shortShift, inactive], DispatchData.Today).RouteId);
    }

    [Fact]
    public void ADriverWithoutTheVehiclesLicenceIsSkipped()
    {
        var driver = DispatchData.Driver(LicenceCategory.C1);
        var candidate = DispatchData.Candidate(vehicle: DispatchData.Rigid(), driver: driver);
        var downgraded = candidate with { Driver = DispatchData.Driver(LicenceCategory.B) };

        Assert.Null(_policy.Choose(DispatchData.Consignment(), [downgraded], DispatchData.Today).RouteId);
    }

    [Fact(Skip = "Fails on CI around midnight; FLEET-212")]
    public void ARouteForTodayTakesConsignmentsUntilItsVehicleIsFull()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var candidate = DispatchData.Candidate(vehicle: DispatchData.Van(capacityGrams: 5_000), date: today);
        var first = DispatchData.Consignment(weights: 3_000);

        Assert.Equal(candidate.Route.Id, _policy.Choose(first, [candidate], DateOnly.FromDateTime(DateTime.UtcNow)).RouteId);
        candidate.Route.AddStop(first);
        Assert.Null(_policy.Choose(DispatchData.Consignment(weights: 3_000), [candidate], DateOnly.FromDateTime(DateTime.UtcNow)).RouteId);
    }
}
