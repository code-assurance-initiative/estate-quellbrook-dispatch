namespace Quellbrook.Dispatch.Domain.Fleet;

public readonly record struct DriverId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

public readonly record struct VehicleId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
