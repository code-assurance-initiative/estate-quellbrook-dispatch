using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Infrastructure.Persistence;

namespace Quellbrook.Dispatch.IntegrationTests;

public sealed class DispatchApiTests(DispatchApiFactory factory) : IClassFixture<DispatchApiFactory>
{
    private static readonly DateOnly ServiceDate = new(2026, 8, 3);

    [Fact]
    public async Task AConsignmentGoesFromRouteToDoorstepThroughTheApi()
    {
        var admin = factory.CreateClient("fleet:admin");
        var dispatcher = factory.CreateClient("dispatch:read", "dispatch:write");
        var driver = await CreatedIdAsync(await admin.PostAsJsonAsync("/fleet/drivers", new { displayName = "Anna K.", depot = "AAR", licence = "b", shiftStart = "07:00", shiftEnd = "15:00" }, TestContext.Current.CancellationToken));
        var vehicle = await CreatedIdAsync(await admin.PostAsJsonAsync("/fleet/vehicles", new { registration = $"QB {Random.Shared.Next(10_000, 99_999)}", depot = "AAR", kind = "van", capacityGrams = 800_000 }, TestContext.Current.CancellationToken));
        var route = await CreatedIdAsync(await dispatcher.PostAsJsonAsync("/routes", new { depot = "AAR", zone = "DK-AAR", serviceDate = ServiceDate, driverId = driver, vehicleId = vehicle, express = false }, TestContext.Current.CancellationToken));
        var consignment = SeedConsignment();

        var assigned = await dispatcher.PostAsJsonAsync($"/consignments/{consignment.Id}/assignment", new { serviceDate = ServiceDate }, TestContext.Current.CancellationToken);
        var started = await dispatcher.PostAsync($"/routes/{route}/start", null, TestContext.Current.CancellationToken);
        var delivered = await dispatcher.PostAsJsonAsync($"/consignments/{consignment.Id}/delivery", new { proof = "signature" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, started.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delivered.StatusCode);
        var view = await dispatcher.GetFromJsonAsync<ConsignmentDto>($"/consignments/by-order/{consignment.OrderId}", TestContext.Current.CancellationToken);
        Assert.Equal("Delivered", view?.Status);
        var board = await dispatcher.GetFromJsonAsync<List<BoardDto>>($"/routes?date={ServiceDate:yyyy-MM-dd}", TestContext.Current.CancellationToken);
        Assert.Contains(board ?? [], entry => entry.RouteId == route && entry.Stops.Count == 1);
    }

    [Fact]
    public async Task InvalidRequestsAreValidationProblems()
    {
        var admin = factory.CreateClient("fleet:admin");
        var dispatcher = factory.CreateClient("dispatch:write");

        var driver = await admin.PostAsJsonAsync("/fleet/drivers", new { displayName = "" }, TestContext.Current.CancellationToken);
        var vehicle = await admin.PostAsJsonAsync("/fleet/vehicles", new { registration = "QB 1", depot = "AAR", kind = "van", capacityGrams = 0 }, TestContext.Current.CancellationToken);
        var route = await dispatcher.PostAsJsonAsync("/routes", new { depot = "AAR" }, TestContext.Current.CancellationToken);
        var assignment = await dispatcher.PostAsJsonAsync($"/consignments/{Guid.NewGuid()}/assignment", new { }, TestContext.Current.CancellationToken);
        var delivery = await dispatcher.PostAsJsonAsync($"/consignments/{Guid.NewGuid()}/delivery", new { }, TestContext.Current.CancellationToken);

        Assert.All([driver, vehicle, route, assignment, delivery], response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
    }

    [Fact]
    public async Task UnknownThingsAreNotFound()
    {
        var dispatcher = factory.CreateClient("dispatch:read", "dispatch:write");

        var byOrder = await dispatcher.GetAsync($"/consignments/by-order/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        var start = await dispatcher.PostAsync($"/routes/{Guid.NewGuid()}/start", null, TestContext.Current.CancellationToken);
        var assign = await dispatcher.PostAsJsonAsync($"/consignments/{Guid.NewGuid()}/assignment", new { serviceDate = ServiceDate }, TestContext.Current.CancellationToken);

        Assert.All([byOrder, start, assign], response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task ScopesAreEnforced()
    {
        var reader = factory.CreateClient("dispatch:read");
        var anonymous = factory.CreateClient();

        var plan = await reader.PostAsJsonAsync("/routes", new { depot = "AAR" }, TestContext.Current.CancellationToken);
        var fleet = await reader.PostAsJsonAsync("/fleet/drivers", new { displayName = "Anna K." }, TestContext.Current.CancellationToken);
        var board = await anonymous.GetAsync("/routes?date=2026-08-03", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, plan.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, fleet.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, board.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsAnswerWithoutATokenWithSecurityHeaders(string path)
    {
        var response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    private Consignment SeedConsignment()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        var consignment = Consignment.Receive(new ConsignmentId(Guid.NewGuid()), Guid.NewGuid(), ServiceLevel.Standard, "DK", "8000", [2_400], DateTimeOffset.UtcNow);
        db.Consignments.Add(consignment);
        db.SaveChanges();
        return consignment;
    }

    private static async Task<Guid> CreatedIdAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IdDto>(TestContext.Current.CancellationToken);
        return body?.Id ?? throw new InvalidOperationException("No id in the response.");
    }

    private sealed record IdDto(Guid Id);

    private sealed record ConsignmentDto(Guid ConsignmentId, string Status);

    private sealed record BoardDto(Guid RouteId, IReadOnlyList<object> Stops);
}
