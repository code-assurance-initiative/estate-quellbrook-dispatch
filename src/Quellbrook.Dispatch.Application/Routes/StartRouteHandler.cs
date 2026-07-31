using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Domain.Common;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Application.Routes;

/// <summary>The driver leaves the depot: the route starts and every consignment on it is out for delivery.</summary>
public sealed class StartRouteHandler(
    IRouteRepository routes,
    IConsignmentRepository consignments,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<OperationResult<RouteId>> HandleAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var route = await routes.FindAsync(new RouteId(routeId), cancellationToken).ConfigureAwait(false);
        if (route is null)
        {
            return OperationResult.NotFound<RouteId>($"Route {routeId} does not exist.");
        }

        var onRoute = await consignments.OnRouteAsync(route.Id, cancellationToken).ConfigureAwait(false);
        try
        {
            route.Start(onRoute, time.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OperationResult.Conflict<RouteId>(exception.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult.Succeeded(route.Id);
    }
}
