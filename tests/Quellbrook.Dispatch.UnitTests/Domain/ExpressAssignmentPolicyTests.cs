using Quellbrook.Dispatch.Domain.Assignment;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.UnitTests.TestSupport;

namespace Quellbrook.Dispatch.UnitTests.Domain;

public sealed class ExpressAssignmentPolicyTests
{
    private readonly ExpressAssignmentPolicy _policy = new(TimeZoneInfo.Utc);

    [Fact]
    public void BeforeTheCutOffAnExpressConsignmentGoesOnTodaysExpressRun()
    {
        var morning = new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero);
        var standard = DispatchData.Candidate(date: DateOnly.FromDateTime(morning.Date));
        var express = DispatchData.Candidate(express: true, date: DateOnly.FromDateTime(morning.Date));

        var decision = _policy.Choose(DispatchData.Consignment(serviceLevel: ServiceLevel.Express), [standard, express], morning);

        Assert.Equal(express.Route.Id, decision.RouteId);
    }

    [Fact]
    public void AfterTheCutOffThereIsNoExpressRoute()
    {
        var afternoon = new DateTimeOffset(2026, 8, 12, 15, 0, 0, TimeSpan.Zero);
        var express = DispatchData.Candidate(express: true, date: DateOnly.FromDateTime(afternoon.Date));

        var decision = _policy.Choose(DispatchData.Consignment(serviceLevel: ServiceLevel.Express), [express], afternoon);

        Assert.Null(decision.RouteId);
    }
}
