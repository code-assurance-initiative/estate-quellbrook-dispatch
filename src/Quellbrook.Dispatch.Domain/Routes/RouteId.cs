namespace Quellbrook.Dispatch.Domain.Routes;

public readonly record struct RouteId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
