namespace PourCast.Scoring.Data;

/// <summary>
/// The observed weather for one hour at a METAR station.
/// </summary>
public class Observation
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public long ObservationId { get; set; }

    /// <summary>
    /// The ICAO code of the station, e.g. EGCC.
    /// </summary>
    public string Station { get; set; } = string.Empty;

    /// <summary>
    /// The start of the observed hour, in UTC.
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// Temperature in °C.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Relative humidity in %, worked out from the temperature and dew point.
    /// </summary>
    public double? Humidity { get; set; }

    /// <summary>
    /// Feels like temperature in °C, worked out the same way as for forecasts.
    /// </summary>
    public double? FeelsLike { get; set; }

    /// <summary>
    /// Wind speed in mph.
    /// </summary>
    public double? WindSpeed { get; set; }

    /// <summary>
    /// Wind gust speed in mph, if a gust was reported.
    /// </summary>
    public double? WindGust { get; set; }

    /// <summary>
    /// Wind direction in degrees, or null if variable or calm.
    /// </summary>
    public double? WindDirection { get; set; }

    /// <summary>
    /// Whether rain, drizzle, showers or thunderstorms were reported during the hour.
    /// </summary>
    public bool Raining { get; set; }
}