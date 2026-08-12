using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Domain;

public sealed class RouteAndFleetTests
{
    [Fact]
    public void StopsAreNumberedInTheOrderTheyAreAddedAndAssignTheConsignment()
    {
        var route = DispatchData.Candidate().Route;
        var first = DispatchData.Consignment();
        var second = DispatchData.Consignment(weights: 5_000);

        route.AddStop(first);
        route.AddStop(second);

        Assert.Equal([1, 2], route.Stops.Select(stop => stop.Sequence));
        Assert.Equal(7_400, route.LoadGrams);
        Assert.Equal(route.Id, second.RouteId);
        Assert.Equal(ConsignmentStatus.Assigned, first.Status);
    }

    [Fact]
    public void StartingARouteSendsItsConsignmentsOutForDelivery()
    {
        var route = DispatchData.Candidate().Route;
        var consignment = DispatchData.Consignment();
        route.AddStop(consignment);

        route.Start([consignment], DispatchData.Now);

        Assert.Equal(RouteStatus.Started, route.Status);
        Assert.Equal(ConsignmentStatus.OutForDelivery, consignment.Status);
        Assert.Throws<DomainException>(() => route.AddStop(DispatchData.Consignment()));
    }

    [Fact]
    public void RemovingAStopRenumbersTheOnesAfterIt()
    {
        var route = DispatchData.Candidate().Route;
        var consignments = new[] { DispatchData.Consignment(), DispatchData.Consignment(), DispatchData.Consignment() };
        foreach (var consignment in consignments)
        {
            route.AddStop(consignment);
        }

        route.RemoveStop(consignments[0].Id);

        Assert.Equal([1, 2], route.Stops.Select(stop => stop.Sequence));
        Assert.Equal(consignments[1].Id, route.Stops[0].ConsignmentId);
        Assert.Throws<DomainException>(() => route.RemoveStop(consignments[0].Id));
    }

    [Fact]
    public void AnEmptyRouteCannotStart() =>
        Assert.Throws<DomainException>(() => DispatchData.Candidate().Route.Start([], DispatchData.Now));

    [Fact]
    public void ARigidVehicleNeedsACategoryC1Driver()
    {
        var rigid = DispatchData.Rigid();

        Assert.Throws<DomainException>(() => Route.Plan(new RouteId(Guid.NewGuid()), "AAR", "DK-AAR", DispatchData.Today,
            DispatchData.Driver(LicenceCategory.B), rigid, express: false));
        Assert.Equal(LicenceCategory.C1, rigid.RequiredLicence);
    }

    [Fact]
    public void AnInactiveDriverCannotBePlanned()
    {
        var driver = DispatchData.Driver();
        driver.Deactivate();

        Assert.Throws<DomainException>(() => Route.Plan(new RouteId(Guid.NewGuid()), "AAR", "DK-AAR", DispatchData.Today,
            driver, DispatchData.Van(), express: false));
    }

    [Fact]
    public void AShiftEndsAfterItStarts() =>
        Assert.Throws<DomainException>(() => DispatchData.Driver(shiftStart: 15, shiftEnd: 7));

    [Fact]
    public void AVehicleCarriesAtMostSevenAndAHalfTonnes() =>
        Assert.Throws<DomainException>(() => DispatchData.Van(capacityGrams: 8_000_000));

    [Fact]
    public void ShiftLengthIsMeasuredInHours() => Assert.Equal(8, DispatchData.Driver(shiftStart: 7, shiftEnd: 15).ShiftHours);
}
