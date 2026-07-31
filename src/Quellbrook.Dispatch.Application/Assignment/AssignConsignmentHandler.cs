using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Application.Assignment;

public sealed record AssignConsignmentCommand(Guid ConsignmentId, DateOnly ServiceDate);

/// <summary>Puts a waiting consignment on the route the assignment policy chooses.</summary>
public sealed class AssignConsignmentHandler(
    IConsignmentRepository consignments,
    IRouteRepository routes,
    StandardAssignmentPolicy standardPolicy,
    IUnitOfWork unitOfWork)
{
    public async Task<OperationResult<RouteId>> HandleAsync(AssignConsignmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var consignment = await consignments.FindAsync(new ConsignmentId(command.ConsignmentId), cancellationToken).ConfigureAwait(false);
        if (consignment is null)
        {
            return OperationResult.NotFound<RouteId>($"Consignment {command.ConsignmentId} does not exist.");
        }

        if (consignment.Status != ConsignmentStatus.AwaitingRoute)
        {
            return OperationResult.Conflict<RouteId>($"Consignment {consignment.Id} is {consignment.Status}.");
        }

        var candidates = await routes.CandidatesAsync(command.ServiceDate, cancellationToken).ConfigureAwait(false);
        var decision = standardPolicy.Choose(consignment, candidates, command.ServiceDate);
        if (decision.RouteId is not { } routeId)
        {
            return OperationResult.Conflict<RouteId>(decision.Reason ?? "No route available.");
        }

        var route = candidates.Single(candidate => candidate.Route.Id == routeId).Route;
        try
        {
            route.AddStop(consignment);
        }
        catch (DomainException exception)
        {
            return OperationResult.Conflict<RouteId>(exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult.Succeeded(routeId);
    }
}
