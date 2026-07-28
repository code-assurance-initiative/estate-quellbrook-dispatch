namespace Quellbrook.Dispatch.Domain.Fleet;

/// <summary>Driving-licence categories, in increasing order of what they allow.</summary>
public enum LicenceCategory
{
    /// <summary>Vans up to 3.5 t.</summary>
    B = 0,

    /// <summary>Light rigid trucks up to 7.5 t.</summary>
    C1 = 1,

    /// <summary>Rigid trucks.</summary>
    C = 2,
}
