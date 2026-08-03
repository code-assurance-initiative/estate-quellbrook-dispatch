using Quellbrook.Dispatch.Api.Contracts;
using Quellbrook.Dispatch.Api.Security;
using Quellbrook.Dispatch.Application.Assignment;
using Quellbrook.Dispatch.Application.Fleet;
using Quellbrook.Dispatch.Application.Queries;
using Quellbrook.Dispatch.Application.Routes;

namespace Quellbrook.Dispatch.Api.Endpoints;

public static class DispatchEndpoints
{
    public static IEndpointRouteBuilder MapDispatchEndpoints(this IEndpointRouteBuilder app)
    {
        var fleet = app.MapGroup("/fleet").RequireAuthorization(AuthorizationPolicies.AdministerFleet);
        fleet.MapPost("/drivers", RegisterDriverAsync);
        fleet.MapPost("/vehicles", RegisterVehicleAsync);

        var routes = app.MapGroup("/routes");
        routes.MapGet("/", BoardAsync).RequireAuthorization(AuthorizationPolicies.ReadDispatch);
        routes.MapPost("/", PlanRouteAsync).RequireAuthorization(AuthorizationPolicies.WriteDispatch);
        routes.MapPost("/{id:guid}/start", StartRouteAsync).RequireAuthorization(AuthorizationPolicies.WriteDispatch);

        var consignments = app.MapGroup("/consignments");
        consignments.MapGet("/by-order/{orderId:guid}", ForOrderAsync).RequireAuthorization(AuthorizationPolicies.ReadDispatch);
        consignments.MapPost("/{id:guid}/assignment", AssignAsync).RequireAuthorization(AuthorizationPolicies.WriteDispatch);
        consignments.MapPost("/{id:guid}/delivery", RecordDeliveryAsync).RequireAuthorization(AuthorizationPolicies.WriteDispatch);
        return app;
    }

    private static async Task<IResult> RegisterDriverAsync(RegisterDriverRequest request, RegisterFleetHandlers handlers, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var command = new RegisterDriverCommand(
            request.DisplayName ?? string.Empty,
            request.Depot ?? string.Empty,
            request.Licence.GetValueOrDefault(),
            request.ShiftStart.GetValueOrDefault(),
            request.ShiftEnd.GetValueOrDefault());
        var result = await handlers.RegisterDriverAsync(command, cancellationToken).ConfigureAwait(false);
        return EndpointResults.Ok(result, id => Results.Created($"/fleet/drivers/{id}", new { id = id.Value }));
    }

    private static async Task<IResult> RegisterVehicleAsync(RegisterVehicleRequest request, RegisterFleetHandlers handlers, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var command = new RegisterVehicleCommand(
            request.Registration ?? string.Empty,
            request.Depot ?? string.Empty,
            request.Kind.GetValueOrDefault(),
            request.CapacityGrams);
        var result = await handlers.RegisterVehicleAsync(command, cancellationToken).ConfigureAwait(false);
        return EndpointResults.Ok(result, id => Results.Created($"/fleet/vehicles/{id}", new { id = id.Value }));
    }

    private static async Task<IResult> BoardAsync(DateOnly date, IDispatchQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.BoardAsync(date, cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> PlanRouteAsync(PlanRouteRequest request, PlanRouteHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var command = new PlanRouteCommand(
            request.Depot ?? string.Empty,
            request.Zone ?? string.Empty,
            request.ServiceDate.GetValueOrDefault(),
            request.DriverId,
            request.VehicleId,
            request.Express);
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return EndpointResults.Ok(result, id => Results.Created($"/routes/{id}", new { id = id.Value }));
    }

    private static async Task<IResult> StartRouteAsync(Guid id, StartRouteHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken).ConfigureAwait(false);
        return EndpointResults.Ok(result, _ => Results.NoContent());
    }

    private static async Task<IResult> ForOrderAsync(Guid orderId, IDispatchQueries queries, CancellationToken cancellationToken)
    {
        var view = await queries.ForOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return view is null ? Results.NotFound() : Results.Ok(view);
    }

    private static async Task<IResult> AssignAsync(Guid id, AssignConsignmentRequest request, AssignConsignmentHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await handler.HandleAsync(new AssignConsignmentCommand(id, request.ServiceDate.GetValueOrDefault()), cancellationToken)
            .ConfigureAwait(false);
        return EndpointResults.Ok(result, routeId => Results.Ok(new { routeId = routeId.Value }));
    }

    private static async Task<IResult> RecordDeliveryAsync(Guid id, RecordDeliveryRequest request, RecordDeliveryHandler handler, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = await handler.HandleAsync(new RecordDeliveryCommand(id, request.Proof.GetValueOrDefault()), cancellationToken)
            .ConfigureAwait(false);
        return EndpointResults.Ok(result, _ => Results.NoContent());
    }
}
