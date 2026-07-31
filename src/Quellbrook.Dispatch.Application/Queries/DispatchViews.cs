namespace Quellbrook.Dispatch.Application.Queries;

public sealed record RouteBoardEntry(
    Guid RouteId,
    string Depot,
    string Zone,
    bool Express,
    string Status,
    string DriverName,
    string VehicleRegistration,
    string VehicleKind,
    int CapacityGrams,
    int LoadGrams,
    IReadOnlyList<RouteBoardStop> Stops);

public sealed record RouteBoardStop(int Sequence, Guid ConsignmentId, Guid OrderId, string PostalCode, int WeightGrams, string Status);

public sealed record ConsignmentView(
    Guid ConsignmentId,
    Guid OrderId,
    string ServiceLevel,
    string Zone,
    string Status,
    Guid? RouteId,
    DateTimeOffset? OutForDeliveryAt,
    DateTimeOffset? DeliveredAt,
    string? Proof);
