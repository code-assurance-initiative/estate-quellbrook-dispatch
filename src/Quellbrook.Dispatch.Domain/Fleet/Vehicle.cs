using Quellbrook.Dispatch.Domain.Common;

namespace Quellbrook.Dispatch.Domain.Fleet;

/// <summary>A delivery vehicle of a depot and the payload it may carry.</summary>
public sealed class Vehicle
{
    private Vehicle(VehicleId id, string registration, string depot, VehicleKind kind, int capacityGrams)
    {
        Id = id;
        Registration = registration;
        Depot = depot;
        Kind = kind;
        CapacityGrams = capacityGrams;
    }

    public VehicleId Id { get; private set; }

    public string Registration { get; private set; }

    public string Depot { get; private set; }

    public VehicleKind Kind { get; private set; }

    public int CapacityGrams { get; private set; }

    /// <summary>The licence a driver needs for this vehicle.</summary>
    public LicenceCategory RequiredLicence => Kind == VehicleKind.Rigid ? LicenceCategory.C1 : LicenceCategory.B;

    public static Vehicle Register(VehicleId id, string registration, string depot, VehicleKind kind, int capacityGrams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(depot);
        if (capacityGrams is <= 0 or > 7_500_000)
        {
            throw new DomainException("A vehicle carries between 1 g and 7.5 t.");
        }

        return new Vehicle(id, registration.Trim().ToUpperInvariant(), depot.Trim().ToUpperInvariant(), kind, capacityGrams);
    }
}
