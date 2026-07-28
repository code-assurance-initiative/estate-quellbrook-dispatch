namespace Quellbrook.Dispatch.Domain.Consignments;

public readonly record struct ConsignmentId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
