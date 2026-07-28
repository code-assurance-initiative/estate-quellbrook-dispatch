namespace Quellbrook.Dispatch.Domain.Routes;

public interface IRouteRepository
{
    Task<Route?> FindAsync(RouteId id, CancellationToken cancellationToken);

    /// <summary>Planned routes of a day, with the vehicle and driver each one uses.</summary>
    Task<IReadOnlyList<Assignment.RouteCandidate>> CandidatesAsync(DateOnly serviceDate, CancellationToken cancellationToken);

    void Add(Route route);
}
