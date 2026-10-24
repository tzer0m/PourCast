namespace PourCast.Scoring;

/// <summary>
/// Configuration for recording and scoring forecasts.
/// </summary>
public class ScoringOptions
{
    /// <summary>
    /// The name of the place forecasts are recorded for.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude forecasts are recorded for, the METAR station's own.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The ICAO code of the METAR station observations come from.
    /// </summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>
    /// The base URL of the aviationweather.gov data API.
    /// </summary>
    public string MetarBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// How many hours of METARs to fetch each time, so gaps from restarts are filled in.
    /// </summary>
    public int MetarHours { get; set; } = 24;
}