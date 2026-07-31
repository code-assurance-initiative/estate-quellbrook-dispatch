using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>The route a consignment should go on, or why there is none.</summary>
public sealed record AssignmentDecision(RouteId? RouteId, string? Reason)
{
    public static AssignmentDecision To(RouteId routeId) => new(routeId, null);

    public static AssignmentDecision None(string reason) => new(null, reason);
}
