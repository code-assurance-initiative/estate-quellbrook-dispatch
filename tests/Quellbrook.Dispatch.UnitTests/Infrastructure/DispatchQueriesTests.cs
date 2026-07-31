using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Infrastructure.Persistence;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Infrastructure;

public sealed class DispatchQueriesTests : IDisposable
{
    private readonly DbFixture _fixture = new();

    [Fact]
    public async Task TheBoardShowsEachRouteOfTheDayWithItsVehicleDriverAndStops()
    {
        var candidate = DispatchData.Candidate();
        var consignment = DispatchData.Consignment(postalCode: "8200");
        candidate.Route.AddStop(consignment);
        await _fixture.SeedAsync(context =>
        {
            context.Drivers.Add(candidate.Driver);
            context.Vehicles.Add(candidate.Vehicle);
            context.Routes.Add(candidate.Route);
            context.Consignments.Add(consignment);
            return 0;
        });
        using var context = _fixture.Context();

        var board = await new DispatchQueries(context).BoardAsync(DispatchData.Today, TestContext.Current.CancellationToken);
        var tomorrow = await new DispatchQueries(context).BoardAsync(DispatchData.Today.AddDays(1), TestContext.Current.CancellationToken);

        var entry = Assert.Single(board);
        Assert.Equal("Anna K.", entry.DriverName);
        Assert.Equal("Van", entry.VehicleKind);
        Assert.Equal(2_400, entry.LoadGrams);
        var stop = Assert.Single(entry.Stops);
        Assert.Equal(("8200", "Assigned"), (stop.PostalCode, stop.Status));
        Assert.Empty(tomorrow);
    }

    [Fact]
    public async Task AConsignmentIsFoundByItsOrder()
    {
        var consignment = DispatchData.Consignment();
        consignment.AssignTo(new Quellbrook.Dispatch.Domain.Routes.RouteId(Guid.NewGuid()));
        consignment.MarkOutForDelivery(DispatchData.Now);
        consignment.RecordDelivery(DeliveryProof.SafePlace, DispatchData.Now.AddHours(2));
        await _fixture.SeedAsync(context => context.Consignments.Add(consignment));
        using var context = _fixture.Context();

        var view = await new DispatchQueries(context).ForOrderAsync(consignment.OrderId, TestContext.Current.CancellationToken);
        var none = await new DispatchQueries(context).ForOrderAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.NotNull(view);
        Assert.Equal(("Delivered", "safe-place"), (view.Status, view.Proof));
        Assert.Null(none);
    }

    public void Dispose() => _fixture.Dispose();
}
