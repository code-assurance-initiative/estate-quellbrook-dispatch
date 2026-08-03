using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;

namespace Quellbrook.Dispatch.Api.Contracts;

public sealed record RegisterDriverRequest(string? DisplayName, string? Depot, LicenceCategory? Licence, TimeOnly? ShiftStart, TimeOnly? ShiftEnd)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new RequestErrors();
        errors.Required(DisplayName, "displayName");
        errors.Required(Depot, "depot");
        errors.Present(Licence, "licence");
        errors.Present(ShiftStart, "shiftStart");
        errors.Present(ShiftEnd, "shiftEnd");
        return errors.ToDictionary();
    }
}

public sealed record RegisterVehicleRequest(string? Registration, string? Depot, VehicleKind? Kind, int CapacityGrams)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new RequestErrors();
        errors.Required(Registration, "registration");
        errors.Required(Depot, "depot");
        errors.Present(Kind, "kind");
        if (CapacityGrams <= 0)
        {
            errors.Add("capacityGrams", "must be positive");
        }

        return errors.ToDictionary();
    }
}

public sealed record PlanRouteRequest(string? Depot, string? Zone, DateOnly? ServiceDate, Guid DriverId, Guid VehicleId, bool Express)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new RequestErrors();
        errors.Required(Depot, "depot");
        errors.Required(Zone, "zone");
        errors.Present(ServiceDate, "serviceDate");
        if (DriverId == Guid.Empty || VehicleId == Guid.Empty)
        {
            errors.Add("driverId/vehicleId", "are required");
        }

        return errors.ToDictionary();
    }
}

public sealed record AssignConsignmentRequest(DateOnly? ServiceDate)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new RequestErrors();
        errors.Present(ServiceDate, "serviceDate");
        return errors.ToDictionary();
    }
}

public sealed record RecordDeliveryRequest(DeliveryProof? Proof)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new RequestErrors();
        errors.Present(Proof, "proof");
        return errors.ToDictionary();
    }
}
