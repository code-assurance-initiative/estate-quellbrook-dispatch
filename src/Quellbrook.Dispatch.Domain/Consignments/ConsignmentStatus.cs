namespace Quellbrook.Dispatch.Domain.Consignments;

public enum ConsignmentStatus
{
    AwaitingRoute = 0,
    Assigned = 1,
    OutForDelivery = 2,
    Delivered = 3,
    Cancelled = 4,
}
