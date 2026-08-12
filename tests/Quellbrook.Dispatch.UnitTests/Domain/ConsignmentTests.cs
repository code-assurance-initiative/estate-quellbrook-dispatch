using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Routes;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Domain;

public sealed class ConsignmentTests
{
    [Fact]
    public void AReceivedConsignmentWaitsForARouteInTheZoneOfItsPostalCode()
    {
        var consignment = DispatchData.Consignment("8200", ServiceLevel.Standard, 1_000, 2_500);

        Assert.Equal(ConsignmentStatus.AwaitingRoute, consignment.Status);
        Assert.Equal("DK-AAR", consignment.Zone);
        Assert.Equal(2, consignment.ParcelCount);
        Assert.Equal(3_500, consignment.TotalWeightGrams);
    }

    [Fact]
    public void AConsignmentWithoutParcelsIsRefused() =>
        Assert.Throws<DomainException>(() => Consignment.Receive(
            new ConsignmentId(Guid.NewGuid()), Guid.NewGuid(), ServiceLevel.Standard, "DK", "8000", [], DispatchData.Now));

    [Fact]
    public void TheLifecycleRaisesOutForDeliveryAndDelivered()
    {
        var consignment = DispatchData.Consignment();
        var routeId = new RouteId(Guid.NewGuid());

        consignment.AssignTo(routeId);
        consignment.MarkOutForDelivery(DispatchData.Now.AddHours(1));
        consignment.RecordDelivery(DeliveryProof.SafePlace, DispatchData.Now.AddHours(3));

        Assert.Equal(ConsignmentStatus.Delivered, consignment.Status);
        Assert.Equal(DeliveryProof.SafePlace, consignment.Proof);
        Assert.Collection(
            consignment.DomainEvents,
            first => Assert.Equal(routeId, Assert.IsType<ConsignmentOutForDelivery>(first).RouteId),
            second => Assert.Equal(DeliveryProof.SafePlace, Assert.IsType<ConsignmentDelivered>(second).Proof));
    }

    [Fact]
    public void AConsignmentCannotBeDeliveredBeforeItIsOutForDelivery()
    {
        var consignment = DispatchData.Consignment();

        Assert.Throws<DomainException>(() => consignment.RecordDelivery(DeliveryProof.Signature, DispatchData.Now));
    }

    [Fact]
    public void AnAssignedConsignmentCannotBeAssignedAgain()
    {
        var consignment = DispatchData.Consignment();
        consignment.AssignTo(new RouteId(Guid.NewGuid()));

        Assert.Throws<DomainException>(() => consignment.AssignTo(new RouteId(Guid.NewGuid())));
    }

    [Fact]
    public void AnAssignedConsignmentCanBeCancelledButNotOnceItIsOut()
    {
        var assigned = DispatchData.Consignment();
        assigned.AssignTo(new RouteId(Guid.NewGuid()));
        var outForDelivery = DispatchData.Consignment();
        outForDelivery.AssignTo(new RouteId(Guid.NewGuid()));
        outForDelivery.MarkOutForDelivery(DispatchData.Now);

        assigned.Cancel();

        Assert.Equal(ConsignmentStatus.Cancelled, assigned.Status);
        Assert.Null(assigned.RouteId);
        Assert.Throws<DomainException>(outForDelivery.Cancel);
    }

    [Theory]
    [InlineData("DK", "1050", "DK-CPH-C")]
    [InlineData("DK", "2300", "DK-CPH-S")]
    [InlineData("DK", "9000", "DK-JUT-N")]
    [InlineData("DK", "3700", "DK-OTHER")]
    [InlineData("SE", "21145", "SE-MAL")]
    [InlineData("NO", "0150", "NO-OSL")]
    [InlineData("DE", "20095", "DE-HAM")]
    [InlineData("NL", "1012", "NL-AMS")]
    [InlineData("NL", "3011", "NL-OTHER")]
    public void PostalCodesMapToTheirDeliveryZone(string country, string postalCode, string zone) =>
        Assert.Equal(zone, DeliveryZones.ZoneFor(country, postalCode));

    [Fact]
    public void ACountryWithoutZonesIsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DeliveryZones.ZoneFor("FR", "75001"));
}
