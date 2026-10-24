namespace PourCast.Models;

/// <summary>
/// A named place that a forecast can be requested for.
/// </summary>
public class Place
{
    /// <summary>
    /// The name of the place.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The region, county or state, if known.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// The country, if known.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// The latitude of the place.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the place.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The IANA time zone of the place, if known.
    /// </summary>
    public string? TimeZone { get; set; }
}