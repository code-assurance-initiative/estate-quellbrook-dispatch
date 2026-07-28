using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.UnitTests.TestSupport;

internal static class DispatchData
{
    public static readonly DateTimeOffset Now = new(2026, 8, 3, 6, 30, 0, TimeSpan.FromHours(2));
    public static readonly DateOnly Today = new(2026, 8, 3);

    public static Consignment Consignment(
        string postalCode = "8000",
        ServiceLevel serviceLevel = ServiceLevel.Standard,
        params int[] weights) =>
        global::Quellbrook.Dispatch.Domain.Consignments.Consignment.Receive(
            new ConsignmentId(Guid.NewGuid()),
            Guid.NewGuid(),
            serviceLevel,
            "DK",
            postalCode,
            weights.Length == 0 ? [2_400] : weights,
            Now);

    public static Driver Driver(LicenceCategory licence = LicenceCategory.B, int shiftStart = 7, int shiftEnd = 15) =>
        global::Quellbrook.Dispatch.Domain.Fleet.Driver.Register(new DriverId(Guid.NewGuid()), "Anna K.", "AAR", licence, new TimeOnly(shiftStart, 0), new TimeOnly(shiftEnd, 0));

    public static Vehicle Van(int capacityGrams = 800_000) =>
        Vehicle.Register(new VehicleId(Guid.NewGuid()), "QB 12 345", "AAR", VehicleKind.Van, capacityGrams);

    public static Vehicle Rigid(int capacityGrams = 3_000_000) =>
        Vehicle.Register(new VehicleId(Guid.NewGuid()), "QB 98 765", "AAR", VehicleKind.Rigid, capacityGrams);

    public static RouteCandidate Candidate(
        string zone = "DK-AAR",
        Vehicle? vehicle = null,
        Driver? driver = null,
        bool express = false,
        DateOnly? date = null)
    {
        var chosenVehicle = vehicle ?? Van();
        var chosenDriver = driver ?? Driver(chosenVehicle.RequiredLicence);
        var route = Route.Plan(new RouteId(Guid.NewGuid()), "AAR", zone, date ?? Today, chosenDriver, chosenVehicle, express);
        return new RouteCandidate(route, chosenVehicle, chosenDriver);
    }
}
