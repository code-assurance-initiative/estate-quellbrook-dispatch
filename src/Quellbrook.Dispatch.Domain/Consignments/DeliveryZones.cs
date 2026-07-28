namespace Quellbrook.Dispatch.Domain.Consignments;

/// <summary>
/// The delivery zones the depots plan routes for, by country and the first two characters of the postal code. The
/// table is maintained by the Fleet team with the depot managers; a district not in it is served from the
/// country's catch-all zone.
/// </summary>
public static class DeliveryZones
{
    public static string ZoneFor(string countryCode, string postalCode)
    {
        ArgumentNullException.ThrowIfNull(postalCode);
        var district = postalCode.Length >= 2 ? postalCode[..2] : postalCode;
        return (countryCode, district) switch
        {
            ("DK", "10" or "11" or "12" or "13" or "14" or "15") => "DK-CPH-C",
            ("DK", "20" or "21" or "22" or "23" or "24" or "25" or "26") => "DK-CPH-S",
            ("DK", "27" or "28" or "29" or "30") => "DK-CPH-N",
            ("DK", "40" or "41" or "42" or "43") => "DK-ZEA",
            ("DK", "50" or "51" or "52" or "53" or "54" or "55") => "DK-FYN",
            ("DK", "60" or "61" or "62" or "63" or "64" or "65" or "66" or "67") => "DK-JUT-S",
            ("DK", "70" or "71" or "72" or "73" or "74" or "75") => "DK-JUT-C",
            ("DK", "80" or "81" or "82" or "83" or "84" or "85" or "86" or "87" or "88") => "DK-AAR",
            ("DK", "90" or "91" or "92" or "93" or "94" or "95" or "96" or "97" or "98" or "99") => "DK-JUT-N",
            ("DK", _) => "DK-OTHER",
            ("SE", "21" or "22" or "23" or "24") => "SE-MAL",
            ("SE", "40" or "41" or "42" or "43") => "SE-GOT",
            ("SE", _) => "SE-OTHER",
            ("NO", "01" or "02" or "03" or "04" or "05" or "06" or "07" or "08" or "09") => "NO-OSL",
            ("NO", _) => "NO-OTHER",
            ("DE", "20" or "21" or "22") => "DE-HAM",
            ("DE", "24" or "25") => "DE-KIE",
            ("DE", _) => "DE-OTHER",
            ("NL", "10" or "11") => "NL-AMS",
            ("NL", _) => "NL-OTHER",
            _ => throw new ArgumentOutOfRangeException(nameof(countryCode), countryCode, "No delivery zones for this country."),
        };
    }
}
