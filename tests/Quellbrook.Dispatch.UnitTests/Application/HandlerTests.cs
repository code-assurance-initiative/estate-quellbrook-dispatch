using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quellbrook.Dispatch.Application;
using Quellbrook.Dispatch.Application.Assignment;
using Quellbrook.Dispatch.Application.Fleet;
using Quellbrook.Dispatch.Application.Intake;
using Quellbrook.Dispatch.Application.Routes;
using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Infrastructure.Persistence;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Application;

public sealed class HandlerTests : IDisposable
{
    private readonly DbFixture _fixture = new();

    private static OrderPlacedMessage Placed(Guid orderId, string serviceLevel = "standard") =>
        new(orderId, serviceLevel, new(new("8000", "DK")), [new(2_400), new(1_100)], DispatchData.Now);

    [Fact]
    public async Task AnAnnouncedOrderBecomesOneConsignmentEvenWhenAnnouncedTwice()
    {
        var orderId = Guid.NewGuid();
        using (var scope = _fixture.Services.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<OrderPlacedHandler>();
            await handler.HandleAsync(Placed(orderId), TestContext.Current.CancellationToken);
            await handler.HandleAsync(Placed(orderId), TestContext.Current.CancellationToken);
        }

        using var context = _fixture.Context();
        var consignment = await context.Consignments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(orderId, consignment.OrderId);
        Assert.Equal(3_500, consignment.TotalWeightGrams);
        Assert.Equal("DK-AAR", consignment.Zone);
    }

    [Fact]
    public async Task AWaitingConsignmentIsAssignedToTheBestStandardRoute()
    {
        var (consignment, candidate) = await SeedRouteAndConsignmentAsync();

        var result = await AssignAsync(consignment.Id.Value);

        Assert.Equal(OperationStatus.Succeeded, result.Status);
        Assert.Equal(candidate.Route.Id, result.Value);
        using var context = _fixture.Context();
        Assert.Equal(ConsignmentStatus.Assigned, (await context.Consignments.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Single((await context.Routes.SingleAsync(TestContext.Current.CancellationToken)).Stops);
    }

    [Fact]
    public async Task WithoutARouteInTheZoneTheAssignmentIsAConflict()
    {
        var (consignment, _) = await SeedRouteAndConsignmentAsync(zone: "DK-FYN");

        var result = await AssignAsync(consignment.Id.Value);

        Assert.Equal(OperationStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task AnUnknownOrAlreadyAssignedConsignmentIsRefused()
    {
        var (consignment, _) = await SeedRouteAndConsignmentAsync();
        await AssignAsync(consignment.Id.Value);

        var again = await AssignAsync(consignment.Id.Value);
        var unknown = await AssignAsync(Guid.NewGuid());

        Assert.Equal(OperationStatus.Conflict, again.Status);
        Assert.Equal(OperationStatus.NotFound, unknown.Status);
    }

    [Fact]
    public async Task StartingARouteAndRecordingADeliveryWriteTheirEventsToTheOutbox()
    {
        var (consignment, candidate) = await SeedRouteAndConsignmentAsync();
        await AssignAsync(consignment.Id.Value);

        using (var context = _fixture.Context())
        {
            var start = await new StartRouteHandler(new EfRouteRepository(context), new EfConsignmentRepository(context), context, _fixture.Time)
                .HandleAsync(candidate.Route.Id.Value, TestContext.Current.CancellationToken);
            Assert.Equal(OperationStatus.Succeeded, start.Status);
        }

        using (var context = _fixture.Context())
        {
            var delivered = await new RecordDeliveryHandler(new EfConsignmentRepository(context), context, _fixture.Time)
                .HandleAsync(new RecordDeliveryCommand(consignment.Id.Value, DeliveryProof.Photo), TestContext.Current.CancellationToken);
            Assert.Equal(OperationStatus.Succeeded, delivered.Status);
        }

        using var reading = _fixture.Context();
        Assert.Equal(
            ["dispatch.consignment-out-for-delivery.v1", "dispatch.consignment-delivered.v1"],
            await reading.OutboxMessages.OrderBy(message => message.OccurredAt).Select(message => message.Type).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains("\"proof\":\"photo\"", (await reading.OutboxMessages.SingleAsync(message => message.Type == "dispatch.consignment-delivered.v1", TestContext.Current.CancellationToken)).Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartingAnUnknownOrEmptyRouteFails()
    {
        var candidate = DispatchData.Candidate();
        await _fixture.SeedAsync(context =>
        {
            context.Drivers.Add(candidate.Driver);
            context.Vehicles.Add(candidate.Vehicle);
            context.Routes.Add(candidate.Route);
            return 0;
        });
        using var context = _fixture.Context();
        var handler = new StartRouteHandler(new EfRouteRepository(context), new EfConsignmentRepository(context), context, _fixture.Time);

        Assert.Equal(OperationStatus.Conflict, (await handler.HandleAsync(candidate.Route.Id.Value, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(OperationStatus.NotFound, (await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task DeliveringAConsignmentThatIsNotOutIsAConflict()
    {
        var (consignment, _) = await SeedRouteAndConsignmentAsync();
        using var context = _fixture.Context();
        var handler = new RecordDeliveryHandler(new EfConsignmentRepository(context), context, _fixture.Time);

        Assert.Equal(OperationStatus.Conflict, (await handler.HandleAsync(new RecordDeliveryCommand(consignment.Id.Value, DeliveryProof.Signature), TestContext.Current.CancellationToken)).Status);
        Assert.Equal(OperationStatus.NotFound, (await handler.HandleAsync(new RecordDeliveryCommand(Guid.NewGuid(), DeliveryProof.Signature), TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task DriversVehiclesAndRoutesAreRegisteredAndPlanned()
    {
        using var context = _fixture.Context();
        var fleet = new RegisterFleetHandlers(new EfFleetRepository(context), context, _fixture.Time);
        var driver = await fleet.RegisterDriverAsync(new RegisterDriverCommand("Anna K.", "aar", LicenceCategory.C1, new(7, 0), new(15, 0)), TestContext.Current.CancellationToken);
        var vehicle = await fleet.RegisterVehicleAsync(new RegisterVehicleCommand("qb 98 765", "aar", VehicleKind.Rigid, 3_000_000), TestContext.Current.CancellationToken);
        var badDriver = await fleet.RegisterDriverAsync(new RegisterDriverCommand("Bo L.", "AAR", LicenceCategory.B, new(15, 0), new(7, 0)), TestContext.Current.CancellationToken);
        var badVehicle = await fleet.RegisterVehicleAsync(new RegisterVehicleCommand("QB 1", "AAR", VehicleKind.Van, 0), TestContext.Current.CancellationToken);

        var plan = new PlanRouteHandler(new EfFleetRepository(context), new EfRouteRepository(context), context, _fixture.Time);
        var route = await plan.HandleAsync(new PlanRouteCommand("AAR", "DK-AAR", DispatchData.Today, driver.Value.Value, vehicle.Value.Value, Express: false), TestContext.Current.CancellationToken);
        var missing = await plan.HandleAsync(new PlanRouteCommand("AAR", "DK-AAR", DispatchData.Today, Guid.NewGuid(), vehicle.Value.Value, Express: false), TestContext.Current.CancellationToken);

        Assert.Equal(OperationStatus.Succeeded, route.Status);
        Assert.Equal(OperationStatus.NotFound, missing.Status);
        Assert.Equal(OperationStatus.Invalid, badDriver.Status);
        Assert.Equal(OperationStatus.Invalid, badVehicle.Status);
        Assert.Equal("QB 98 765", (await context.Vehicles.SingleAsync(TestContext.Current.CancellationToken)).Registration);
    }

    [Fact]
    public async Task PlanningWithADriverWithoutTheLicenceIsInvalid()
    {
        var driver = DispatchData.Driver(LicenceCategory.B);
        var rigid = DispatchData.Rigid();
        await _fixture.SeedAsync(context =>
        {
            context.Drivers.Add(driver);
            context.Vehicles.Add(rigid);
            return 0;
        });
        using var context = _fixture.Context();
        var plan = new PlanRouteHandler(new EfFleetRepository(context), new EfRouteRepository(context), context, _fixture.Time);

        var result = await plan.HandleAsync(new PlanRouteCommand("AAR", "DK-AAR", DispatchData.Today, driver.Id.Value, rigid.Id.Value, Express: false), TestContext.Current.CancellationToken);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    private async Task<OperationResult<Quellbrook.Dispatch.Domain.Routes.RouteId>> AssignAsync(Guid consignmentId)
    {
        using var context = _fixture.Context();
        var handler = new AssignConsignmentHandler(new EfConsignmentRepository(context), new EfRouteRepository(context), new StandardAssignmentPolicy(), context);
        return await handler.HandleAsync(new AssignConsignmentCommand(consignmentId, DispatchData.Today), TestContext.Current.CancellationToken);
    }

    private Task<(Consignment Consignment, RouteCandidate Candidate)> SeedRouteAndConsignmentAsync(string zone = "DK-AAR")
    {
        var candidate = DispatchData.Candidate(zone: zone);
        var consignment = DispatchData.Consignment();
        return _fixture.SeedAsync(context =>
        {
            context.Drivers.Add(candidate.Driver);
            context.Vehicles.Add(candidate.Vehicle);
            context.Routes.Add(candidate.Route);
            context.Consignments.Add(consignment);
            return (consignment, candidate);
        });
    }

    public void Dispose() => _fixture.Dispose();
}
