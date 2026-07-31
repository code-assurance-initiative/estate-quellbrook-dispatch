using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Domain.Assignment;

/// <summary>A planned route together with the vehicle and driver it uses, as the assignment policies see it.</summary>
public sealed record RouteCandidate(Route Route, Vehicle Vehicle, Driver Driver)
{
    public int RemainingGrams => Vehicle.CapacityGrams - Route.LoadGrams;
}
