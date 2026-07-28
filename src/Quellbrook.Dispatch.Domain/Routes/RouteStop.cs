using Quellbrook.Dispatch.Domain.Consignments;

namespace Quellbrook.Dispatch.Domain.Routes;

/// <summary>One consignment on a route, in delivery order.</summary>
public sealed record RouteStop(ConsignmentId ConsignmentId, int WeightGrams, int Sequence);
